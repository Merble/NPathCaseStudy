
/*
 *  Locations of the "input.json" and "output.json" files are:
 *      NPathCaseStudy\bin\Debug\net6.0
 *
 *  The program does not need additional builds. Running the program is sufficient
 */

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NPathCaseStudy
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            const string inputFilePath = "input.json";
            const string outputFilePath = "output.json";

            var simulation = new CarWashingSimulation();
            simulation.LoadData(inputFilePath);         // Start the program and Get the data
            simulation.RunSimulation();                 // Use the data
            simulation.SaveResults(outputFilePath);     // Save the data and Finish the program

            Console.WriteLine("Simulation completed successfully!");
        }
    }
    
    public class CarWashingSimulation  // All Simulation Stages are here
    {
        private List<Car> _cars;
        private List<WashingSystem> _washingSystems;

        public void LoadData(string inputFilePath)  // Get All Data from input.json file and process them
        {
            var inputJson = File.ReadAllText(inputFilePath);
            
            dynamic data = JsonConvert.DeserializeObject(inputJson) ?? throw new InvalidOperationException();

            _cars = new List<Car>();
            foreach (var carData in data.vehicles)
            {
                var car = new Car
                {
                    Id = carData.id,
                    Dirtiness = carData.dirtiness,
                    EffectivenessLevels = new Dictionary<string, float>()
                };

                foreach (var effectivenessLevelData in carData.effectiveness_levels)
                {
                    var washType = ((JProperty)effectivenessLevelData).Name;
                    var effectiveness = (float) effectivenessLevelData.Value;
                    car.EffectivenessLevels.Add(washType, effectiveness);
                }

                _cars.Add(car);
            }

            _washingSystems = new List<WashingSystem>();
            foreach (var washingSystemData in data.washing_systems)
            {
                string systemType = washingSystemData.rule;

                WashingSystem washingSystem = systemType switch
                {
                    "random" => new RandomWashSystem(),
                    "ordered" => new OrderedWashSystem(),
                    _ => throw new Exception($"Invalid washing system rule: {systemType}")
                };

                washingSystem.Id = washingSystemData.id;
                washingSystem.WashType = washingSystemData.wash_type;
                washingSystem.WashingRule = washingSystemData.rule;

                _washingSystems.Add(washingSystem);
            }
        }

        public void RunSimulation()   // Simulation Runs here and calculations are done here
        {
            var allCarsClean = false;
            
            while (!allCarsClean)
            {
                allCarsClean = true;
                
                foreach (var washingSystem in _washingSystems)
                {
                    var carToWash = washingSystem.SelectCarToWash(_cars);

                    if (carToWash == null) continue;
                    
                    var washEffectiveness = washingSystem.GetWashEffectiveness(carToWash);
                    var removedDirt = washingSystem.CleaningLevel * washEffectiveness;
                    carToWash.Dirtiness -= removedDirt;
                    
                    //Console.WriteLine("Wash system" + washingSystem.Id +" Cleaning level before: " + washingSystem.CleaningLevel + " and Car" + carToWash.Id + " dirtiness before: " + carToWash.Dirtiness);
                    switch (washingSystem.WashingRule)
                    {
                        case "ordered":
                            washingSystem.CleaningLevel -= 20;
                            break;
                        case "random":
                            washingSystem.CleaningLevel -= 10;
                            break;
                    }

                    //Console.WriteLine("Wash system" + washingSystem.Id +" Cleaning level after: " + washingSystem.CleaningLevel + " and Car" + carToWash.Id + " dirtiness after: " + carToWash.Dirtiness);
                    
                    if (washingSystem.CleaningLevel <= 0) 
                        washingSystem.CleaningLevel = 0;
                    
                    if (carToWash.Dirtiness < 0)
                        carToWash.Dirtiness = 0;

                    if (carToWash.Dirtiness > 0)
                        allCarsClean = false; // At least one car is still dirty
                }
            }
        }
        
        public void SaveResults(string outputFilePath) // Saving the results to output file and finishing the program
        {
            var results = new List<dynamic>();
            foreach (var car in _cars)
            {
                dynamic carResult = new
                {
                    id = car.Id,
                    final_dirtiness = (int) car.Dirtiness
                };
                results.Add(carResult);
            }

            dynamic output = new
            {
                vehicles = results
            };

            string outputJson = JsonConvert.SerializeObject(output, Formatting.Indented);
            File.WriteAllText(outputFilePath, outputJson);
        }
    }
    
    public class Car
    {
        public int Id { get; set; }
        public float Dirtiness { get; set; }
        public Dictionary<string, float> EffectivenessLevels { get; set; }
    }

    public abstract class WashingSystem
    {
        public int Id { get; set; }
        public string WashType { get; set; }
        public string WashingRule { get; set; }
        public float CleaningLevel { get; set; } = 100;

        public abstract Car SelectCarToWash(List<Car> cars);
        public abstract float GetWashEffectiveness(Car car);
    }

    public class RandomWashSystem : WashingSystem
    {
        private readonly Random _randomNumber;
        private List<Car> _washedCars = new List<Car>();

        public RandomWashSystem()
        {
            _randomNumber = new Random();
        }

        public override Car SelectCarToWash(List<Car> cars)
        {
            if (_washedCars.Count >= cars.Count) return null;
            Car selectedCar;
            
            do
            {
                var index = _randomNumber.Next(cars.Count);
                selectedCar = cars[index];

                if (_washedCars.Contains(selectedCar))
                {
                    continue;
                }

                _washedCars.Add(selectedCar);
                break;

            } while (true);
            
            return selectedCar;
        }

        public override float GetWashEffectiveness(Car car)
        {
            return car.EffectivenessLevels[WashType];
        }
    }

    public class OrderedWashSystem : WashingSystem
    {
        private int _currentIndex;

        public OrderedWashSystem()
        {
            _currentIndex = 0;
        }

        public override Car SelectCarToWash(List<Car> cars)
        {
            if (_currentIndex >= cars.Count) return null;
            
            var selectedCar = cars[_currentIndex];
            _currentIndex++;
            return selectedCar;
        }

        public override float GetWashEffectiveness(Car car)
        {
            return car.EffectivenessLevels[WashType];
        }
    }
}


