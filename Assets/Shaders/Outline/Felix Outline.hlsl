/* HLSL Post Processing outline shader
   Thanks to Aske (ASKE) for some guiding words on how to make this
*/

#ifndef FELIXHLSLINCLUDE_INCLUDED
#define FELIXHLSLINCLUDE_INCLUDED

void SobelOutline_float(
    UnityTexture2D tex, float2 uv, float sensitivity,
    out float Out
){
    // Set up kernels
    int3x3 Kx =
    {
        1, 0, -1,
        2, 0, -2,
        1, 0, -1
    };
    int3x3 Ky =
    {
        1, 2, 1,
        0, 0, 0,
        -1, -2, -1
    };
    
    float Gx = 0.0f;
    float Gy = 0.0f;

    for (int i = -1; i <= 1; i++)
    {
        for (int j = -1; j <= 1; j++)
        {
            float2 uv_ = uv + sensitivity * float2(i, j);

            float l = tex2D(tex, uv).xyz;
            
            Gx += Kx[i + 1][j + 1] * l;
            Gy += Ky[i + 1][j + 1] * l;
        }
    }
    
    // Get the magnitude/length of this
    float Mag = sqrt(Gx * Gx + Gy * Gy);
    
    Out = Mag;
}

void SobelOutline_half(
    UnityTexture2D tex, half2 uv, half sensitivity,
    out half Out
)
{
    // Set up kernels
    int3x3 Kx =
    {
        1, 0, -1,
        2, 0, -2,
        1, 0, -1
    };
    int3x3 Ky =
    {
        1, 2, 1,
        0, 0, 0,
        -1, -2, -1
    };
    
    half Gx = 0.0f;
    half Gy = 0.0f;

    for (int i = -1; i <= 1; i++)
    {
        for (int j = -1; j <= 1; j++)
        {
            half2 uv_ = uv + sensitivity * half2(i, j);

            half l = UNITY_SAMPLE_TEX2D(tex, uv).xyz;
            
            Gx += Kx[i + 1][j + 1] * l;
            Gy += Ky[i + 1][j + 1] * l;
        }
    }
    
    // Get the magnitude/length of this
    half Mag = sqrt(Gx * Gx + Gy * Gy);
    
    Out = Mag;
}

#endif // FELIXHLSLINCLUDE_INCLUDED