Shader "MirrorsAndPortals/Portals/DepthMaskWithOffset"
{
    SubShader{
        // Render the mask after regular geometry, but before masked geometry and
        // transparent things.
        Tags {"Queue" = "Geometry-1" }
 
        // Don't draw in the RGBA channels; just the depth buffer
        ColorMask 0
        ZWrite On
        ZTest Always

        // Do nothing specific in the pass:
        Pass {
            Offset 0,-10000000
        }
    }
}