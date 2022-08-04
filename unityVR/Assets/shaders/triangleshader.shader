Shader "Custom/SpherePoint" {
    Properties{
        _Color("Color", Color) = (1,1,1,1)
        _Radius("Sphere Radius", float) = 0.01
    }
        SubShader{
       Tags { "RenderType" = "Opaque" }
       LOD 200
       Pass {
           CGPROGRAM
           #pragma vertex vert
           #pragma fragment frag
           #pragma geometry geom
           #pragma target 4.0                  // Use shader model 3.0 target, to get nicer looking lighting
           #include "UnityCG.cginc"
           struct vertexIn {
               float4 pos : POSITION;
               float4 color : COLOR;
               float3 normal : NORMAL;
           };
           struct vertexOut {
               float4 pos : POSITION;
               float4 color : COLOR0;
               float3 normal : NORMAL;
           };
           struct geomOut {
               float4 pos : POSITION;
               float4 color : COLO0R;
               float3 normal : NORMAL;
           };
           //Vertex shader: computes normal wrt camera
           vertexOut vert(vertexIn i) {
               vertexOut o;
               o.pos = UnityObjectToClipPos(i.pos);
               //o.pos = mul(unity_ObjectToWorld, i.pos);
               o.color = i.color;
               //o.normal = normalize(_WorldSpaceCameraPos - mul(unity_ObjectToWorld, o.pos).xyz); //normal is towards the camera
               o.normal = ObjSpaceViewDir(o.pos);
               return o;
           }
           static const fixed SQRT3_6 = sqrt(3) / 6;
           float _Radius;
           float4 _Color;
           [maxvertexcount(3)]
           void geom(point vertexOut i[1], inout TriangleStream<geomOut> OutputStream)
           {
               geomOut p;
               float a = _ScreenParams.x / _ScreenParams.y; // aspect ratio
               float s = _Radius;
               float s2 = s * SQRT3_6 * a;

               p.color = i[0].color * _Color;
               p.normal = float3(0, 0, 1);
               p.pos = i[0].pos + float4(0, s2 * 2, 0, 0);
               OutputStream.Append(p);
               p.pos = i[0].pos + float4(-s * 0.5f, -s2, 0, 0);
               OutputStream.Append(p);
               p.pos = i[0].pos + float4(s * 0.5f, -s2, 0, 0);
               OutputStream.Append(p);
               OutputStream.RestartStrip();
           }
           float4 frag(geomOut i) : COLOR
           {
               return i.color;
           }
           ENDCG
       }
    }
        FallBack "Diffuse"
}