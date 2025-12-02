using System;
using System.Collections.Generic; // Ya no es necesario si listadoClientes se quita
using System.Text;
using System.Text.RegularExpressions; // Ya no es necesario
using System.Threading;
using System.Net;
using System.Net.Sockets;
using Protocolo; // Asegúrate de tener la referencia al proyecto Protocolo

namespace Servidor
{
    class Servidor
    {
        private static TcpListener escuchador;
        // La lista listadoClientes ya NO está aquí, se movió a la clase Protocolo
        // private static Dictionary<string, int> listadoClientes ... (ELIMINAR)

        // Instancia de Protocolo: Debe ser estática o un Singleton para manejar el estado (listadoClientes).
        private static readonly Protocolo.Protocolo manejadorProtocolo = new Protocolo.Protocolo();

        static void Main(string[] args)
        {
            try
            {
                // ... (código para iniciar escuchador se mantiene) ...
                escuchador = new TcpListener(IPAddress.Any, 8080);
                // CORRECCIÓN: El puerto debe ser el mismo que usa el cliente, 8080.
                escuchador.Start();
                Console.WriteLine("Servidor inició en el puerto 8080...");

                while (true)
                {
                    TcpClient cliente = escuchador.AcceptTcpClient();
                    Console.WriteLine("Cliente conectado, puerto: {0}", cliente.Client.RemoteEndPoint.ToString());
                    Thread hiloCliente = new Thread(ManipuladorCliente);
                    hiloCliente.Start(cliente);
                }
            }
            // ... (catch y finally se mantienen) ...
            catch (SocketException ex)
            {
                Console.WriteLine("Error de socket al iniciar el servidor: " +
                    ex.Message);
            }
            finally
            {
                escuchador?.Stop();
            }
        }

        private static void ManipuladorCliente(object obj)
        {
            TcpClient cliente = (TcpClient)obj;
            NetworkStream flujo = null;
            try
            {
                flujo = cliente.GetStream();
                byte[] bufferTx;
                byte[] bufferRx = new byte[1024];
                int bytesRx;

                while ((bytesRx = flujo.Read(bufferRx, 0, bufferRx.Length)) > 0)
                {
                    string mensajeRx =
                        Encoding.UTF8.GetString(bufferRx, 0, bytesRx);

                    // 1. Procesar Pedido (se mantiene la función estática en Pedido)
                    Pedido pedido = Pedido.Procesar(mensajeRx);
                    Console.WriteLine("Se recibió: " + pedido);

                    string direccionCliente =
                        cliente.Client.RemoteEndPoint.ToString();

                    // 2. DELEGAR la resolución al objeto Protocolo
                    Respuesta respuesta = manejadorProtocolo.ResolverPedido(pedido, direccionCliente);
                    Console.WriteLine("Se envió: " + respuesta);

                    // 3. Enviar Respuesta
                    bufferTx = Encoding.UTF8.GetBytes(respuesta.ToString());
                    flujo.Write(bufferTx, 0, bufferTx.Length);
                }

            }
            // ... (catch y finally se mantienen) ...
            catch (Exception ex) // Cambiado a Exception para capturar errores de flujo
            {
                // CORRECCIÓN del problema de Sockets en Servidor
                // Manejar Thread-Safety: La excepción puede ser causada al intentar escribir
                // en un NetworkStream cerrado por el cliente. 
                Console.WriteLine("Conexión con cliente cerrada o error: " + ex.Message);
            }
            finally
            {
                // La gestión de cerrar los recursos se mantiene.
                flujo?.Close();
                cliente?.Close();
            }
        }

        // Las funciones ResolverPedido, ValidarPlaca, ObtenerIndicadorDia, ContadorCliente se ELIMINAN de aquí.

    }
}