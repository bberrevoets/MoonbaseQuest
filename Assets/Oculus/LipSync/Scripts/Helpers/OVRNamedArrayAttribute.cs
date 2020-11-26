// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using UnityEngine;

// Adds support for a named array attribute in the editor
public class OVRNamedArrayAttribute : PropertyAttribute
{
    public readonly string[] names;

    public OVRNamedArrayAttribute(string[] names)
    {
        this.names = names;
    }
}
