using System.Collections.Generic;

namespace Kitchenbuilder.Core.Models
{
    public class Kitchen
    {
        public Floor? Floor { get; set; }
        public List<Wall> Walls { get; set; } = new();
        public Base? Base { get; set; }

        // בעתיד אפשר להוסיף מקרר, כיריים וכו'
    }
}
