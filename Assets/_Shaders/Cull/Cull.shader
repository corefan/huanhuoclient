﻿Shader "Custom/Cull" {
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Color ("Rim Color", Color) = (0, 0, 0, 0)
	}

	SubShader {
	
	// ---- forward rendering base pass:
	Pass {
		Cull Front
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#include "UnityCG.cginc"

		float4 _Color;

		struct v2f{
		  float4 pos:SV_POSITION;
		}; 

		v2f vert(appdata_base v){
			v2f o;
			o.pos=v.vertex;
			o.pos.xyz+=v.normal*0.03;
			o.pos=mul(UNITY_MATRIX_MVP,o.pos);
			return o;
		}

		float4 frag(v2f i):COLOR{
			return _Color;
		}
		ENDCG
	}
	  Tags { "RenderType"="Opaque" }
		LOD 100
	Pass{
		Tags {"LightMode" = "ForwardBase"}
	  Cull BACK
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"
			#include "Lighting.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv);
				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
	  }
	}
}