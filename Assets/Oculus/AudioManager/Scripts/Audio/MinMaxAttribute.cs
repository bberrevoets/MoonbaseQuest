// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using UnityEngine;

namespace OVR
{
    /*
    -----------------------
    
     MinMaxAttribute
    
    -----------------------
    */
    public class MinMaxAttribute : PropertyAttribute
    {
        public float max           = 1.0f;
        public float maxDefaultVal = 1.0f;
        public float min           = 0.0f;
        public float minDefaultVal = 1.0f;

        public MinMaxAttribute(float minDefaultVal, float maxDefaultVal, float min, float max)
        {
            this.minDefaultVal = minDefaultVal;
            this.maxDefaultVal = maxDefaultVal;
            this.min           = min;
            this.max           = max;
        }
    }
} // namespace OVR
