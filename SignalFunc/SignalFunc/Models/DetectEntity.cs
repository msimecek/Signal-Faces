using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalFunc.Models
{
    public class DetectEntity : TableEntity
    {
        public int Faces { get; set; }

        public DetectEntity() { }

        public DetectEntity(string fileName, int faces)
        {
            this.PartitionKey = " ";
            this.RowKey = fileName;
            this.Faces = faces;
        }

        public enum DetectState
        {
            NotProcessed = -1,
            NoFaces = 0
        }
    }
}
