Shader "Perception/LocalPosition"
{
    Properties
    {
        _Center ("Center", Vector) = (0, 0, 0, 0)
        _Size ("Size", Vector) = (1, 1, 1, 0)
        _Override ("Override", Color) = (0, 0, 0, 1)
    }

    HLSLINCLUDE

    #pragma target 4.5
    #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

    //enable GPU instancing support
    #pragma multi_compile_instancing

    ENDHLSL

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Tags { "LightMode" = "SRP" }

            Blend Off
            ZWrite On
            ZTest LEqual

            Cull Back

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            float4 _Center;
            float4 _Size;
            fixed4 _Override;

            struct app_to_vertex
            {
                float4 vertex : POSITION;
            };

            struct vertex_to_fragment
            {
                float4 vertex : SV_POSITION;
                float4 vertex_object_space : COLOR;
            };

            vertex_to_fragment vert (app_to_vertex input)
            {
                vertex_to_fragment o;
                o.vertex = UnityObjectToClipPos(input.vertex);
                o.vertex_object_space = (input.vertex - _Center) / _Size + float4(0.5, 0.5, 0.5, 1);
                o.vertex_object_space.w = 1;
                o.vertex_object_space = o.vertex_object_space * _Override;
                return o;
            }

            float4 frag (vertex_to_fragment input) : SV_Target
            {
                return input.vertex_object_space;
            }
            ENDCG
        }
    }
}
