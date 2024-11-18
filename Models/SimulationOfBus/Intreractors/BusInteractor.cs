
using Microsoft.Identity.Client;
using System.Collections.Concurrent;
using System.Runtime.ConstrainedExecution;
using WPFTasks.Models.SimulationOfBus.Data;
using WPFTasks.Models.SimulationOfBus.Repositories;
using WPFTasks.SimulationArchitecture;
using BusNumber = System.UInt32;

namespace WPFTasks.Models.SimulationOfBus.Intreractors
{
    public class BusInteractor : Interactor
    {
        private BusRepository rep = null!;
        private CancellationTokenSource cts = new();

        public Action<Bus>? OnStartBus;

        public override void OnStart()
        {
            rep = WPFTasks.Models.Simulation.GetRepository<BusRepository>();
        }

        public async void StartBusesOfNumber(BusNumber num)
        {
            // Получаем количество автобусов с указанным номером
            if (!rep.BusOfNumberCount.TryGetValue(num, out uint count))
                return;

            // Локальный метод для запуска автобуса
            async Task StartNextBusAsync()
            {
                if (count == 0)
                    return;

                // Забираем автобус с парковки и добавляем в рабочие
                var bus = rep.SwapParkToWork(num);
                if (bus == null)
                    return;

                bus.OnBusCameToStop += ComeToStop;

                await StartBusAsync(bus);
            }

            // Событие "Автобус прибыл на остановку"
            async void ComeToStop(Stop _)
            {
                count--;
                var bus = rep.GetParkingBus(num);
                if (bus != null)
                {
                    // Отписываемся от события и запускаем следующий автобус
                    bus.OnBusCameToStop -= ComeToStop;
                    await StartNextBusAsync();
                }
            }

            // Запускаем первую итерацию
            if (count > 0)
            {
                await StartNextBusAsync();
            }
        }

        public void StartAllBuses()
        {
            var tasks = new ConcurrentBag<Task>();

            // Параллельно запускаем автобусы всех номеров
            foreach (var num in rep.BusOfNumberCount.Keys)
            {
                tasks.Add(Task.Run(() => StartBusesOfNumber(num)));
            }
        }

        private async Task StartBusAsync(Bus bus)
        {
            OnStartBus?.Invoke(bus);

            // Ждем завершения работы автобуса с поддержкой отмены
            try
            {
                await bus.StartWorking(cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Обрабатываем отмену операции
            }
        }

        public override void OnDispose()
        {
            cts.Cancel(); 
            cts.Dispose();
        }
    }
}
