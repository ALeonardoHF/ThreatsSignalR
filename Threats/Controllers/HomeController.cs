using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Threats.Hubs;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Threats.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private static Random random = new Random();
        private static ConcurrentDictionary<int, (int gpu, int core)> processGpuMapping = new ConcurrentDictionary<int, (int, int)>();
        private static int gpuCount = 4;
        private static int coresPerGpu = 2;
        private readonly IHubContext<ProcessHub> _hubContext;

        public HomeController(IHubContext<ProcessHub> hubContext, ILogger<HomeController> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public IActionResult Index()
        {
            ViewData["Title"] = "Generate Process";
            return View();
        }

        [HttpPost]
        public IActionResult GenerateProcess()
        {
            Thread thread = new Thread(async () => await StartProcess());
            thread.Start();

            return RedirectToAction("Index");
        }

        private async Task StartProcess()
        {
            string dummyProcessPath = @"C:\Github\Threading\DummyProcess\bin\Debug\net8.0\DummyProcess.exe"; // Ruta relativa al directorio de salida

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = dummyProcessPath,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            int duration = random.Next(20000, 30001); // Duración del proceso en milisegundos (entre 20 y 30 segundos)
            int gpuConsumption = random.Next(1, 101); // Consumo de la GPU en porcentaje

            startInfo.Arguments = $"{duration} {gpuConsumption}";

            try
            {
                Process process = Process.Start(startInfo);

                if (process == null)
                {
                    Console.WriteLine("Failed to start process.");
                    return;
                }

                var assignedGpuCore = AssignGpuCore();
                processGpuMapping[process.Id] = assignedGpuCore;

                string startMessage = $"Proceso {process.Id} generado con duración de {duration / 1000} segundos, consumo de GPU del {gpuConsumption}%, asignado a GPU {assignedGpuCore.gpu} núcleo {assignedGpuCore.core}.";
                Console.WriteLine(startMessage); // Log de depuración
                await _hubContext.Clients.All.SendAsync("ReceiveMessage", startMessage);

                // Esperar a que el proceso termine y mostrar su ID
                process.WaitForExit();
                string endMessage = $"Proceso {process.Id} ha finalizado. Estaba asignado a GPU {assignedGpuCore.gpu} núcleo {assignedGpuCore.core}.";
                Console.WriteLine(endMessage); // Log de depuración
                await _hubContext.Clients.All.SendAsync("ReceiveMessage", endMessage);

                // Remover el proceso de la asignación
                processGpuMapping.TryRemove(process.Id, out _);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting process: {ex.Message}");
            }
        }

        private (int gpu, int core) AssignGpuCore()
        {
            int assignedGpu = random.Next(0, gpuCount);
            int assignedCore = random.Next(0, coresPerGpu);

            return (assignedGpu, assignedCore);
        }
    }
}