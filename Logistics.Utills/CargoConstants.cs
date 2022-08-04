using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logistics.Utills
{
    public class CargoConstants
    {
        public const string CollectionName = "cargo";
        public const string schemaVersion = "schemaVersion";
        public const string schemaVersionValue = "V1";
        public const string Location = "location";
        public const string Destination = "destination";
        public const string Courier = "courier";
        public const string Status = "status";
        public const string Received = "received";
        public const string Delivered = "delivered";
        public const string DeliveredAt = "deliveredAt";
        public const string InProgress = "in process";
        public const string CourierSource = "courierSource";
        public const string CourierDestination = "courierDestination";
        public const string TransitType = "transitType";
        public const string CargoTransitTypeRegional = "Regional";
        public const string CargoTransitTypeInternational = "International";
        public const string CargoTransitTypeBackup = "Backup";
        public const string Duration = "duration";
        public const string CourierTrackingHistory = "courierTrackingHistory";
        public const string Plane = "assignedPlane";
        public const string PackageLocation = "cargoLocation";
        public const string Time = "time";
        public const string Created = "created";
    }
}
