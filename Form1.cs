using System;
using System.Drawing;
using System.Windows.Forms;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.IO;
using System.Threading.Tasks;
using OpenCvSharp;
using System.Text.Json.Serialization;

namespace FireDetectorApp
{
    public partial class Form1 : Form
    {
        private static readonly HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        private const string GeminiApiEndpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash-latest:generateContent";
        private const string GeminiApiKey = "Apy Key aqui"; // Reemplaza con tu clave API
        private const double MinConfidenceThreshold = 0.1;
        private VideoCapture capture;
        private bool isStreaming = false;

        public Form1()
        {
            InitializeComponent();
            btnLoadImage.Text = "Capturar Imagen";
            btnLoadImage.Click += BtnCaptureImage_Click;
            this.Load += Form1_Load;
        }

        private void Form1_Load(object? sender, EventArgs e)
        {
            StartStreaming();
        }

        private async void StartStreaming()
        {
            capture = new VideoCapture(0);
            if (!capture.IsOpened())
            {
                MessageBox.Show("No se pudo abrir la cámara web.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblResult.Text = "Error.";
                return;
            }

            isStreaming = true;
            lblResult.Text = "Vista previa en curso...";

            while (isStreaming)
            {
                try
                {
                    using (var frame = new Mat())
                    {
                        if (!capture.Read(frame) || frame.Empty())
                        {
                            continue;
                        }

                        Bitmap bitmap = null;
                        try
                        {
                            bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(frame);
                            if (bitmap == null)
                            {
                                throw new InvalidOperationException("La conversión de Mat a Bitmap falló.");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error al convertir frame a Bitmap: {ex.Message}");
                            continue;
                        }

                        pictureBoxPreview.Invoke((Action)(() =>
                        {
                            if (pictureBoxPreview.Image != null)
                                pictureBoxPreview.Image.Dispose();
                            pictureBoxPreview.Image = bitmap;
                        }));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error en el streaming: {ex.Message}");
                }
                await Task.Delay(33); // Aproximadamente 30 FPS
            }
        }

        private async void BtnCaptureImage_Click(object? sender, EventArgs e)
        {
            isStreaming = false;

            try
            {
                if (capture == null || !capture.IsOpened())
                {
                    MessageBox.Show("La cámara web no está disponible.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    lblResult.Text = "Error.";
                    return;
                }

                lblResult.Text = "Capturando imagen...";

                await Task.Delay(100);
                using (var frame = new Mat())
                {
                    if (!capture.Read(frame) || frame.Empty())
                    {
                        MessageBox.Show("No se pudo capturar la imagen.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        lblResult.Text = "Error.";
                        return;
                    }

                    Console.WriteLine($"Frame capturado - Empty: {frame.Empty()}, Size: {frame.Width}x{frame.Height}");

                    Bitmap bitmap = null;
                    try
                    {
                        bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(frame);
                        if (bitmap == null)
                        {
                            throw new InvalidOperationException("La conversión de Mat a Bitmap falló.");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error al convertir la imagen a Bitmap: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        lblResult.Text = "Error.";
                        return;
                    }

                    if (pictureBoxPreview.Image != null)
                        pictureBoxPreview.Image.Dispose();
                    pictureBoxPreview.Image = bitmap;

                    lblResult.Text = "Analizando imagen...";
                    // Limpiar el resultado persistente anterior antes de un nuevo análisis
                    lblPersistentResult.Invoke((Action)(() => lblPersistentResult.Text = ""));
                    bool fireDetected = await DetectFireAsync(frame);

                    // Actualizar lblPersistentResult con el resultado del análisis
                    lblPersistentResult.Invoke((Action)(() => {
                        lblPersistentResult.Text = fireDetected ? "Resultado: ¡Incendio detectado!" : "Resultado: No se detectó incendio.";
                        lblPersistentResult.ForeColor = fireDetected ? Color.Red : Color.Green;
                    }));
                    lblResult.Text = "Análisis completado."; // Indicar que el análisis finalizó
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al capturar o analizar la imagen: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblResult.Text = "Error.";
                if (pictureBoxPreview.Image != null)
                {
                    pictureBoxPreview.Image.Dispose();
                    pictureBoxPreview.Image = null;
                }
            }
            finally
            {
                if (capture != null && capture.IsOpened())
                {
                    isStreaming = true;
                    await Task.Run(() => StartStreaming());
                }
            }
        }

        private async Task<bool> DetectFireAsync(Mat frame)
{
    Console.WriteLine($"Iniciando DetectFireAsync con GeminiApiKey: {GeminiApiKey.Substring(0, 5)}..."); // Muestra los primeros 5 caracteres
    if (string.IsNullOrEmpty(GeminiApiKey))
    {
        MessageBox.Show("Por favor, configura una clave API válida de Gemini.", "Configuración Requerida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        Console.WriteLine("Error: Clave API vacía.");
        return false;
    }

    int maxRetries = 3;
    int retryDelaySeconds = 20;

    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            byte[] imageBytes = frame.ToBytes(".jpg");
            Console.WriteLine($"Tamaño de la imagen enviada: {imageBytes.Length} bytes (Intento {attempt}/{maxRetries})");

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new { text = @"
                                Analiza esta imagen y determina si contiene fuego (incluyendo pequeñas llamas como las de un encendedor) o humo, indicadores de un posible incendio.
                                Proporciona únicamente una respuesta en formato JSON puro con los siguientes campos:
                                {
                                    ""fire_detected"": boolean,
                                    ""confidence"": float (entre 0 y 1),
                                    ""description"": string
                                }
                                Ejemplos:
                                {
                                    ""fire_detected"": true,
                                    ""confidence"": 0.95,
                                    ""description"": ""Se detectaron llamas visibles de un encendedor.""
                                }
                                {
                                    ""fire_detected"": false,
                                    ""confidence"": 0.1,
                                    ""description"": ""No se detectaron llamas ni humo visibles.""
                                }
                                No incluyas texto adicional, explicaciones ni formato Markdown (como ```json ... ```). Solo devuelve el JSON.
                            " },
                            new
                            {
                                inline_data = new
                                {
                                    mime_type = "image/jpeg",
                                    data = Convert.ToBase64String(imageBytes)
                                }
                            }
                        }
                    }
                }
            };

            string jsonRequest = JsonSerializer.Serialize(requestBody);
            Console.WriteLine("Solicitud JSON enviada: " + jsonRequest);

            var content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");

            string apiUrl = $"{GeminiApiEndpoint}?key={GeminiApiKey}";
            Console.WriteLine($"Enviando solicitud a: {apiUrl}");
            HttpResponseMessage response = await client.PostAsync(apiUrl, content);

            Console.WriteLine($"Código de estado de la respuesta: {(int)response.StatusCode} ({response.StatusCode})");
            string responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Contenido de la respuesta: {responseContent}");

            if (response.IsSuccessStatusCode)
            {
                var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseContent);
                var resultText = geminiResponse?.Candidates?[0]?.Content?.Parts?[0]?.Text;

                if (string.IsNullOrEmpty(resultText))
                {
                    Console.WriteLine("No se recibió una respuesta válida de Gemini. ResultText está vacío.");
                    lblResult.Text = "No se recibió una respuesta válida de Gemini.";
                    return false;
                }

                Console.WriteLine($"Respuesta de Gemini recibida: {resultText}");

                // ✨ Nueva lógica para limpiar y parsear el JSON recibido
                string cleanJson = resultText
                    .Replace("```json", "")
                    .Replace("```", "")
                    .Trim();

                try
                {
                    var fireResult = JsonSerializer.Deserialize<GeminiResult>(cleanJson);
                    if (fireResult != null)
                    {
                        // Actualizar lblPersistentResult con la descripción del resultado
                        lblPersistentResult.Invoke((Action)(() => {
                            lblPersistentResult.Text = fireResult.Description ?? "Sin descripción.";
                            lblPersistentResult.ForeColor = fireResult.FireDetected ? Color.Red : Color.Green;
                        }));
                        Console.WriteLine($"Descripción: {fireResult.Description}");
                        // Devolver true si se detectó fuego y la confianza es suficiente
                        return fireResult.FireDetected && fireResult.Confidence >= MinConfidenceThreshold;
                    }
                    else
                    {
                        lblPersistentResult.Invoke((Action)(() => {
                            lblPersistentResult.Text = "No se pudo interpretar la respuesta.";
                            lblPersistentResult.ForeColor = Color.Orange;
                        }));
                        return false;
                    }
                }
                catch (JsonException ex)
                {
                    MessageBox.Show($"Error al analizar JSON interno: {ex.Message}", "Error de análisis", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Console.WriteLine($"Error al analizar JSON interno: {ex.Message}{Environment.NewLine}Texto limpio: {cleanJson}");
                    lblPersistentResult.Invoke((Action)(() => {
                        lblPersistentResult.Text = "Error al interpretar la respuesta.";
                        lblPersistentResult.ForeColor = Color.Orange;
                    }));
                    return false;
                }
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                if (attempt == maxRetries)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Error de cuota de la API de Gemini: {response.StatusCode}{Environment.NewLine}{errorContent}", "Error de API", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Console.WriteLine($"Error de API: {response.StatusCode} - {errorContent}");
                    lblPersistentResult.Invoke((Action)(() => {
                        lblPersistentResult.Text = $"Error API: {response.StatusCode}";
                        lblPersistentResult.ForeColor = Color.Orange;
                    }));
                    return false;
                }
                Console.WriteLine($"Error 429: Demasiadas solicitudes. Reintentando en {retryDelaySeconds} segundos... (Intento {attempt}/{maxRetries})");
                await Task.Delay(retryDelaySeconds * 1000);
                continue;
            }
            else
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                MessageBox.Show($"Error de la API de Gemini: {response.StatusCode}{Environment.NewLine}{errorContent}", "Error de API", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine($"Error de API: {response.StatusCode} - {errorContent}");
                lblPersistentResult.Invoke((Action)(() => {
                    lblPersistentResult.Text = $"Error API: {response.StatusCode}";
                    lblPersistentResult.ForeColor = Color.Orange;
                }));
                return false;
            }
        }
        catch (HttpRequestException ex)
        {
            MessageBox.Show($"Error al contactar la API de Gemini: {ex.Message}", "Error de Conexión", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Console.WriteLine($"Excepción HttpRequestException: {ex.Message}{Environment.NewLine}Stack Trace: {ex.StackTrace}");
            lblPersistentResult.Invoke((Action)(() => {
                lblPersistentResult.Text = $"Error Conexión: {ex.Message.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? ex.Message}";
                lblPersistentResult.ForeColor = Color.Orange;
            }));
            return false;
        }
        catch (JsonException ex)
        {
            MessageBox.Show($"Error al deserializar la respuesta de la API: {ex.Message}", "Error de Deserialización", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Console.WriteLine($"Excepción JsonException: {ex.Message}{Environment.NewLine}Stack Trace: {ex.StackTrace}");
            lblPersistentResult.Invoke((Action)(() => {
                lblPersistentResult.Text = $"Error Deserialización: {ex.Message.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? ex.Message}";
                lblPersistentResult.ForeColor = Color.Orange;
            }));
            return false;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error inesperado al contactar la API de Gemini: {ex.Message}", "Error Inesperado", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Console.WriteLine($"Excepción General en DetectFireAsync: {ex.Message}{Environment.NewLine}Stack Trace: {ex.StackTrace}");
            lblPersistentResult.Invoke((Action)(() => {
                lblPersistentResult.Text = $"Error Inesperado: {ex.Message.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? ex.Message}";
                lblPersistentResult.ForeColor = Color.Orange;
            }));
            return false;
        }
    }

    lblPersistentResult.Invoke((Action)(() => {
        lblPersistentResult.Text = "Error: Máximo de reintentos alcanzado.";
        lblPersistentResult.ForeColor = Color.Orange;
    }));
    return false;
}

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            isStreaming = false;
            capture?.Release();
            capture?.Dispose();
        }
    }

    public class GeminiResponse
{
    [JsonPropertyName("candidates")]
    public List<Candidate>? Candidates { get; set; }
}

public class Candidate
{
    [JsonPropertyName("content")]
    public Content? Content { get; set; }
}

public class Content
{
    [JsonPropertyName("parts")]
    public List<Part>? Parts { get; set; }

    [JsonPropertyName("role")]
    public string? Role { get; set; }
}

public class Part
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

public class GeminiResult
{
    [JsonPropertyName("fire_detected")]
    public bool FireDetected { get; set; }

    [JsonPropertyName("confidence")]
    public float Confidence { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}
}
