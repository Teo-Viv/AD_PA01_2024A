using System;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net.Sockets;
using Protocolo;

namespace Cliente
{
    public partial class FrmValidador : Form
    {
        private TcpClient remoto;
        private NetworkStream flujo; // El flujo ya no es usado por el método HazOperacion

        // Instancia de Protocolo: 
        private readonly Protocolo.Protocolo clienteProtocolo = new Protocolo.Protocolo();
        public FrmValidador()
        {
            InitializeComponent();  
        }

        private void FrmValidador_Load(object sender, EventArgs e)
        {
            try
            {
                // Conexión inicial al Servidor. El puerto debe ser 8080 (consistencia con Servidor)
                remoto = new TcpClient("127.0.0.1", 8080);
                // El flujo se usaba antes en HazOperacion, ahora se maneja dentro de Protocolo.HazOperacion
            }
            catch (SocketException ex)
            {
                MessageBox.Show("No se puedo establecer conexión " + ex.Message,
                    "ERROR");
            }
            finally
            {
                // Quitar el cierre aquí, ya que el TcpClient debe permanecer abierto
                // para que el cliente siga enviando peticiones.
                // flujo?.Close(); 
                // remoto?.Close(); 
            }

            panPlaca.Enabled = false;
            chkLunes.Enabled = false;
            chkMartes.Enabled = false;
            chkMiercoles.Enabled = false;
            chkJueves.Enabled = false;
            chkViernes.Enabled = false;
            chkDomingo.Enabled = false;
            chkSabado.Enabled = false;
        }

        private void btnIniciar_Click(object sender, EventArgs e)
        {
            string usuario = txtUsuario.Text;
            string contraseña = txtPassword.Text;
            if (usuario == "" || contraseña == "")
            {
                MessageBox.Show("Se requiere el ingreso de usuario y contraseña",
                    "ADVERTENCIA");
                return;
            }

            Pedido pedido = new Pedido
            {
                Comando = "INGRESO",
                Parametros = new[] { usuario, contraseña }
            };

            // Usar la clase Protocolo para la operación
            Respuesta respuesta = clienteProtocolo.HazOperacion(pedido, remoto);
            if (respuesta == null)
            {
                MessageBox.Show("Hubo un error de conexión", "ERROR");
                return;
            }

            // ... (lógica de manejo de respuesta se mantiene) ...
            if (respuesta.Estado == "OK" && respuesta.Mensaje == "ACCESO_CONCEDIDO")
            {
                panPlaca.Enabled = true;
                panLogin.Enabled = false;
                MessageBox.Show("Acceso concedido", "INFORMACIÓN");
                txtModelo.Focus();
            }
            else if (respuesta.Estado == "NOK" && respuesta.Mensaje == "ACCESO_NEGADO")
            {
                panPlaca.Enabled = false;
                panLogin.Enabled = true;
                MessageBox.Show("No se pudo ingresar, revise credenciales",
                    "ERROR");
                txtUsuario.Focus();
            }
        }

        private void btnConsultar_Click(object sender, EventArgs e)
        {
            string modelo = txtModelo.Text;
            string marca = txtMarca.Text;
            string placa = txtPlaca.Text;
            
            Pedido pedido = new Pedido
            {
                Comando = "CALCULO",
                Parametros = new[] { modelo, marca, placa }
            };

            Respuesta respuesta = clienteProtocolo.HazOperacion(pedido, remoto);
            if (respuesta == null)
            {
                MessageBox.Show("Hubo un error de conexión", "ERROR");
                return;
            }

            if (respuesta.Estado == "NOK")
            {
                MessageBox.Show("Error en la solicitud.", "ERROR");
                chkLunes.Checked = false;
                chkMartes.Checked = false;
                chkMiercoles.Checked = false;
                chkJueves.Checked = false;
                chkViernes.Checked = false;
            }
            else
            {
                var partes = respuesta.Mensaje.Split(' ');
                MessageBox.Show("Se recibió: " + respuesta.Mensaje,
                    "INFORMACIÓN");
                byte resultado = Byte.Parse(partes[1]);
                switch (resultado)
                {
                    case 0b00100000:
                        chkLunes.Checked = true;
                        chkMartes.Checked = false;
                        chkMiercoles.Checked = false;
                        chkJueves.Checked = false;
                        chkViernes.Checked = false;
                        break;
                    case 0b00010000:
                        chkMartes.Checked = true;
                        chkLunes.Checked = false;
                        chkMiercoles.Checked = false;
                        chkJueves.Checked = false;
                        chkViernes.Checked = false;
                        break;
                    case 0b00001000:
                        chkMiercoles.Checked = true;
                        chkLunes.Checked = false;
                        chkMartes.Checked = false;
                        chkJueves.Checked = false;
                        chkViernes.Checked = false;
                        break;
                    case 0b00000100:
                        chkJueves.Checked = true;
                        chkLunes.Checked = false;
                        chkMartes.Checked = false;
                        chkMiercoles.Checked = false;
                        chkViernes.Checked = false;
                        break;
                    case 0b00000010:
                        chkViernes.Checked = true;
                        chkLunes.Checked = false;
                        chkMartes.Checked = false;
                        chkMiercoles.Checked = false;
                        chkJueves.Checked = false;
                        break;
                    default:
                        chkLunes.Checked = false;
                        chkMartes.Checked = false;
                        chkMiercoles.Checked = false;
                        chkJueves.Checked = false;
                        chkViernes.Checked = false;
                        break;
                }
            }
        }

        private void btnNumConsultas_Click(object sender, EventArgs e)
        {
            String mensaje = "hola";
            
            Pedido pedido = new Pedido
            {
                Comando = "CONTADOR",
                Parametros = new[] { mensaje }
            };

            Respuesta respuesta = clienteProtocolo.HazOperacion(pedido, remoto);
            if (respuesta == null)
            {
                MessageBox.Show("Hubo un error de conexión", "ERROR");
                return;
            }

            if (respuesta.Estado == "NOK")
            {
                MessageBox.Show("Error en la solicitud.", "ERROR");

            }
            else
            {
                var partes = respuesta.Mensaje.Split(' ');
                MessageBox.Show("El número de pedidos recibidos en este cliente es " + partes[0],
                    "INFORMACIÓN");
            }
        }

        private void FrmValidador_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (remoto != null)
                if (remoto.Connected)
                    remoto.Close();
        }
    }
}
