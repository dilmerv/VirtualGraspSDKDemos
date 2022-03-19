/*Shader "Shaders/Material/RimLight" {
	Properties{
		_MainTex("Albedo (RGB)", 2D) = "white" {}
	_Color("Color", Color) = (1, 1, 1, 1)
		_Rim("Rim", Range(0,1)) = 0.1
	}
		SubShader{
		Tags{ "RenderType" = "Opaque" }
		CGPROGRAM
#pragma surface surf Standard

		sampler2D _MainTex;
	float4 _Color;
	float _Rim;
#define RIM (1.0 - _Rim)

	struct Input {
		float2 uv_MainTex;
		float3 worldNormal;
		float3 viewDir;
	};

	void surf(Input IN, inout SurfaceOutputStandard o) {

		// Calculate the angle between normal and view direction
		float diff = 1.0 - dot(IN.worldNormal, IN.viewDir);

		// Cut off the diff to the rim size using the RIM value.
		diff = step(RIM, diff) * diff;

		// Smooth value
		float value = step(RIM, diff) * (diff - RIM) / RIM;

		// Sample texture and add rim color
		float3 rgb = tex2D(_MainTex, IN.uv_MainTex).rgb;
		o.Albedo = (float3)value* _Color + rgb;
	}
	ENDCG
	}

		FallBack "Diffuse"
}*/
Shader "Test/RimLight" {
	Properties{
		_MainTex("Texture", 2D) = "white" {}
	_RimColor("Rim Color", Color) = (0.26,0.19,0.16,0.0)
		_RimPower("Rim Power", Range(0.5,8.0)) = 3.0
	}
		SubShader{
		Tags{ "RenderType" = "Opaque" }
		CGPROGRAM
#pragma surface surf Lambert
		struct Input {
		float2 uv_MainTex;
		float3 viewDir;
	};

	sampler2D _MainTex;
	float4 _RimColor;
	float _RimPower;
	void surf(Input IN, inout SurfaceOutput o) {
		o.Albedo = tex2D(_MainTex, IN.uv_MainTex).rgb;
		half rim = 1.0 - saturate(dot(normalize(IN.viewDir), o.Normal));
		o.Emission = _RimColor.rgb * pow(rim, _RimPower);
	}
	ENDCG
	}
		Fallback "Diffuse"
}