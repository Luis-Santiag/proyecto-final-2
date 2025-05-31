using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageCaptureClientUI
{
    public partial class Form1 : Form
    {



        
        private const string apiKey = "1RPSf8QTKjCwlVMeWUDl"; // Reemplaza con tu clave de API
        private const string modelEndpoint = "fire-detection-8icva-hrmgx/1"; // Reemplaza con el endpoint de tu modelo

        public Form1()
        {
            InitializeComponent();
        }

        private async void btnProcesarImagen_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    string rutaImagen = ofd.FileName;
                    pictureBox1.Image = Image.FromFile(rutaImagen);
                    lblResultado.Text = "Procesando imagen...";

                    var resultado = await DetectarFuegoRoboflow(rutaImagen);

                    if (resultado.success)
                    {
                        lblResultado.Text = $"🔥 Se detectó fuego con confianza {resultado.confianza:P2}";
                    }
                    else
                    {
                        lblResultado.Text = "❌ No se detectó fuego en la imagen.";
                    }
                }
            }
        }

        private async Task<(bool success, float confianza)> DetectarFuegoRoboflow(string rutaImagen)
        {
            
            try
            {
                using (var client = new HttpClient())
                {
                    var requestUri = $"fire-detection-8icva-hrmgx/1";

                    using (var content = new MultipartFormDataContent())
                    {
                        var imageContent = new ByteArrayContent(File.ReadAllBytes(rutaImagen));
                        imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
                        content.Add(imageContent, "file", Path.GetFileName(rutaImagen));

                        var response = await client.PostAsync(requestUri, content);
                        string responseString = await response.Content.ReadAsStringAsync();

                        if (!response.IsSuccessStatusCode)
                        {
                            MessageBox.Show($"Error HTTP: {response.StatusCode}\n{responseString}");
                            return (false, 0f);
                        }

                        JObject jsonObj = JObject.Parse(responseString);
                        var predictions = jsonObj["predictions"] as JArray;

                        if (predictions == null || predictions.Count == 0)
                            return (false, 0f);

                        var mejorPrediccion = predictions.OrderByDescending(p => (float)p["confidence"]).First();
                        float confidence = (float)mejorPrediccion["confidence"];

                        return (true, confidence);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al procesar la imagen: {ex.Message}");
                return (false, 0f);
            }
        }
    }
}