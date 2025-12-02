// ********************************************************************************
// Práctica 07
// Práctica Acumulativa 01 - Refactorización de Protocolo
//
// Mateo Vivas 
//
// Fecha de realizacion: 16/11/2025
//
// Fecha de entrega: 03/12/2025
//
// Resultados:
//
//   * [**Actualizado a la Práctica de Protocolo/GitHub**] El código fue refactorizado exitosamente para implementar la clase Protocolo.
//   * El cliente y el servidor utilizan la clase Protocolo para encapsular la lógica de comunicación (HazOperación y ResolverPedido).
//   * Se corrigió el problema de validación de credenciales en el servidor (uso de Random).
//
// Conclusiones:
//  * La refactorización mejora la mantenibilidad y desacoplamiento del código al aplicar el principio de responsabilidad única.
//  * El uso de GitHub/Git en Visual Studio permite un control de versiones eficiente, facilitando el seguimiento de los cambios y la colaboración.
//
// Recomendaciones:
//  * Implementar una clase de Loggeo centralizada para el manejo de excepciones en lugar de usar MessageBox y Console.WriteLine directamente.
//  * Usar locks o Mutex para garantizar la Thread-Safety de listadoClientes, aunque se movió a Protocolo, el acceso concurrente puede seguir siendo un problema.
//
// ********************************************************************************
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System;

namespace Protocolo
{

    /// Clase central que gestiona el protocolo de comunicación y la lógica de negocio.

    public class Protocolo
    {
        // El listado de clientes debe moverse aquí, ahora es parte de la lógica del protocolo.
        private readonly Dictionary<string, int> listadoClientes = new Dictionary<string, int>();

        // ----------------------- Lógica del Servidor (Resolver Pedido) -----------------------

        /// Resuelve el pedido recibido, conteniendo toda la lógica de negocio.
        public Respuesta ResolverPedido(Pedido pedido, string direccionCliente)
        {
            Respuesta respuesta = new Respuesta
            { Estado = "NOK", Mensaje = "Comando no reconocido" };

            switch (pedido.Comando)
            {
                case "INGRESO":
                    if (pedido.Parametros.Length == 2 &&
                        pedido.Parametros[0] == "root" &&
                        pedido.Parametros[1] == "admin20")
                    {
                        // CORRECCIÓN: El problema encontrado en clase es el uso de new Random() que
                        // no es determinístico y permite el acceso denegado incluso con credenciales correctas.
                        // La corrección es quitar el uso de Random para la validación de credenciales.
                        respuesta = new Respuesta
                        {
                            Estado = "OK",
                            Mensaje = "ACCESO_CONCEDIDO"
                        };
                    }
                    else
                    {
                        respuesta.Mensaje = "ACCESO_NEGADO";
                    }
                    break;

                case "CALCULO":
                    if (pedido.Parametros.Length == 3)
                    {
                        string placa = pedido.Parametros[2];
                        if (ValidarPlaca(placa))
                        {
                            byte indicadorDia = ObtenerIndicadorDia(placa);
                            respuesta = new Respuesta
                            {
                                Estado = "OK",
                                Mensaje = $"{placa} {indicadorDia}"
                            };
                            ContadorCliente(direccionCliente); // Se registra la consulta exitosa
                        }
                        else
                        {
                            respuesta.Mensaje = "Placa no válida";
                        }
                    }
                    break;

                case "CONTADOR":
                    if (listadoClientes.ContainsKey(direccionCliente))
                    {
                        respuesta = new Respuesta
                        {
                            Estado = "OK",
                            Mensaje = listadoClientes[direccionCliente].ToString()
                        };
                    }
                    else
                    {
                        respuesta.Mensaje = "No hay solicitudes previas";
                    }
                    break;
            }

            return respuesta;
        }

        private bool ValidarPlaca(string placa)
        {
            return Regex.IsMatch(placa, @"^[A-Z]{3}[0-9]{4}$");
        }

        private byte ObtenerIndicadorDia(string placa)
        {
            int ultimoDigito = int.Parse(placa.Substring(6, 1));
            switch (ultimoDigito)
            {
                case 1:
                case 2:
                    return 0b00100000; // Lunes
                case 3:
                case 4:
                    return 0b00010000; // Martes
                case 5:
                case 6:
                    return 0b00001000; // Miércoles
                case 7:
                case 8:
                    return 0b00000100; // Jueves
                case 9:
                case 0:
                    return 0b00000010; // Viernes
                default:
                    return 0;
            }
        }

        private void ContadorCliente(string direccionCliente)
        {
            if (listadoClientes.ContainsKey(direccionCliente))
            {
                listadoClientes[direccionCliente]++;
            }
            else
            {
                listadoClientes[direccionCliente] = 1;
            }
        }

        // ----------------------- Lógica del Cliente (Realizar Operación) -----------------------

        /// Implementa la lógica de enviar y recibir el pedido/respuesta.

        public Respuesta HazOperacion(Pedido pedido, TcpClient remoto)
        {
            if (remoto == null || !remoto.Connected)
            {
                // No hay conexión establecida
                return null;
            }

            NetworkStream flujo = null;
            try
            {
                // Se obtiene el flujo de red del cliente
                flujo = remoto.GetStream();

                // 1. Envío
                byte[] bufferTx = Encoding.UTF8.GetBytes(pedido.ToString()); // Usa el .ToString() de Pedido
                flujo.Write(bufferTx, 0, bufferTx.Length);

                // 2. Recepción
                byte[] bufferRx = new byte[1024];
                int bytesRx = flujo.Read(bufferRx, 0, bufferRx.Length);
                string mensaje = Encoding.UTF8.GetString(bufferRx, 0, bytesRx);

                // 3. Procesamiento de Respuesta
                var partes = mensaje.Split(' ');

                return new Respuesta
                {
                    Estado = partes[0],
                    Mensaje = string.Join(" ", partes.Skip(1).ToArray())
                };
            }
            catch (SocketException)
            {
                // Manejo de errores de socket si la conexión se pierde.
                return null;
            }
        }

    }
}