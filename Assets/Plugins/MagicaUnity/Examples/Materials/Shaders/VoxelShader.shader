Shader "Custom/Voxel Opaque" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_Metallic("Metallic", Range(0,1)) = 0.0
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Emission ("Emission", Range(0,1)) = 0.0
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows vertex:vert

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex;
		struct appdata {
			float4 vertex : POSITION;
			float3 normal : NORMAL;
			float4 color : COLOR;
			float4 texcoords0 : TEXCOORD0;
			float4 texcoords1 : TEXCOORD1;
			float4 _material : TEXCOORD2;
			UNITY_VERTEX_INPUT_INSTANCE_ID
		};
		struct Input {
			float2 uv_MainTex;
			float4 color : COLOR;
			float4 texcoords1 : TEXCOORD1;

			float4 _material : TEXCOORD2;
		};

		// x: smoothness
		// y: emission
		// z: spec
		// w: metallic
		void vert(inout appdata v, out Input o) {
			UNITY_SETUP_INSTANCE_ID(v);
			UNITY_INITIALIZE_OUTPUT(Input, o);
			o.uv_MainTex = v.texcoords0;
			o.texcoords1 = v.texcoords1;
			o._material = v._material;
			o.color = v.color;
		}

		UNITY_INSTANCING_BUFFER_START(Props)
			UNITY_DEFINE_INSTANCED_PROP(half, _Glossiness)
			UNITY_DEFINE_INSTANCED_PROP(half, _Metallic)
			UNITY_DEFINE_INSTANCED_PROP(half, _Emission)
			UNITY_DEFINE_INSTANCED_PROP(fixed4, _Color)
		UNITY_INSTANCING_BUFFER_END(Props)

		void surf (Input IN, inout SurfaceOutputStandard o) {
			// Albedo comes from a texture tinted by color
			fixed4 c = IN.color * UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
			o.Albedo = pow(c.rgb, 2.2);
			// Metallic and smoothness come from slider variables
			o.Metallic = IN._material.w * UNITY_ACCESS_INSTANCED_PROP(Props, _Metallic);
			o.Smoothness = IN._material.x * UNITY_ACCESS_INSTANCED_PROP(Props, _Glossiness);
			o.Emission = IN._material.y * UNITY_ACCESS_INSTANCED_PROP(Props, _Emission) * c.rgb;
			o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
