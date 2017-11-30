using Microsoft.ProjectOxford.Face.Contract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalFunc.Models
{
    public class DetectionResult
    {
        public const string SOURCE_TEST = "test";
        public const string SOURCE_PRODUCTION = "production";
        
        /// <summary>
        /// Base64 encoded photo.
        /// </summary>
        public string Image { get; set; }
        
        /// <summary>
        /// When was the image taken.
        /// </summary>
        public DateTime DateTime { get; set; }
        
        /// <summary>
        /// Source of the data for backend filtering. 
        /// Expected: test | production
        /// </summary>
        public string Source { get; set; }
        
        /// <summary>
        /// List of top tags detected on the image.
        /// </summary>
        public List<Tag> Tags { get; set; }

        /// <summary>
        /// Position of the face on the picture.
        /// </summary>
        public FaceRectangle FaceRectangle { get; set; }
    }

    public class Tag
    {
        public string Name { get; set; }
        public dynamic Value { get; set; }
        public double Confidence { get; set; }

        public Tag(string name, dynamic value, double confidence = 1)
        {
            Name = name;
            Value = value;
            Confidence = confidence;
        }
    }
}
