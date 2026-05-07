using System.Collections.Generic;

namespace SaigonRide.Models.entities
{
    public class Station
    {
        public int Id { get; set; }
        public string LocationName { get; set; }
        public int MaxCapacity { get; set; }
        public int CurrentInventory { get; set; } 
        public double Latitude { get; set; }
        public double Longitude { get; set; } 
        public ICollection<Vehicle> Vehicles { get; set; }
        public bool IsAvailable { get; set; } = true; // Stations are available by default
    }
}