Shader "Unlit/VideoShader"
{
    Properties
    {
        _MainTex("", 2D) = "gray" {}

        // Basic adjustment
        _Brightness("Brightness", Range(-1, 1)) = 0
        _Contrast("Contrast", Range(-1, 2)) = 1
        _Saturation("Saturation", Range(0, 2)) = 1

        // Color balance
        [HideInInspector] _Temperature("Temperature", Range(-1, 1)) = 0
        [HideInInspector] _Tint("Tint", Range(-1, 1)) = 0
        _ColorBalance("Color Balance", Vector) = (1, 1, 1, 1)

        // Keying
        [Toggle(_KEYING)] _Keying("Keying", Float) = 0
        [HideInInspector] _KeyColor("Key Color", Color) = (0, 1, 0, 0)
        _KeyCgCo("Key Color Vector", Vector) = (0, 0, 0, 0)
        _KeyThreshold("Key Threshold", Range(0, 1)) = 0.5
        _KeyTolerance("Key Tolerence", Range(0, 1)) = 0.2
        _SpillRemoval("Spill Removal", Range(0, 1)) = 0.5

        // Transform
        _Trim("Trim", Vector) = (0, 0, 0, 0)
        [HideInInspector] _TrimParams("Trim Params", Vector) = (0, 0, 0, 0)
        _Scale("Scale", Vector) = (1, 1, 1, 1)
        _Offset("Offset", Vector) = (0, 0, 0, 0)

        // Final tweaks
        [Gamma] _FadeToColor("Fade To Color", Color) = (0, 0, 0, 0)
        _Opacity("Opacity", Range(0, 1)) = 1

        // Blend mode control
        [HideInInspector] _SrcBlend("", Int) = 1
        [HideInInspector] _DstBlend("", Int) = 0
        [HideInInspector] _ZWrite("", Int) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        Pass
        {
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_Zwrite]

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ UNITY_COLORSPACE_GAMMA
            #pragma shader_feature _KEYING

            #include "VideoShader.cginc"

            v2f_img vert(appdata_img v)
            {
                v2f_img o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TransformUV(v.texcoord);
                return o;
            }

            half4 frag(v2f_img i) : SV_Target
            {
                return ProcAmp(i.uv);
            }

            ENDCG
        }
    }
}
