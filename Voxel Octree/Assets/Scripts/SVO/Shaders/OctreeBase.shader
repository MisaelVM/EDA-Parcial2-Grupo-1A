﻿Shader "Octree/OctreeBase"
{
	Properties
	{
		_Shininess ("Shininess", Range(0, 1))=1
		[MainTexture] [NoScaleOffset] _Volume ("OctreeVolume", 3D) = "" {}
	}
	SubShader
	{
		Tags { "Queue"="AlphaTest" }
		
		Cull Front

		Pass
		{
			Tags { 
				"RenderType"="AlphaTest"
			}
			
			ZWrite On

			HLSLPROGRAM

			#include "UnityCG.cginc"
			#include "GeometryRayCast.hlsl"
			#include "AutoLight.cginc"

			#pragma vertex vert
			#pragma fragment frag

			uniform Texture2D<float3> hit_pos;
			
			float _Shininess;
			Texture3D<int> _Volume;
			sampler2D _CameraDepthTexture;

			struct v2f
			{
				float4 pos : SV_POSITION;
				float3 world_pos : TEXCOORD0;
				float4 screen_pos : TEXCOORD1;
				float3 ray_origin : TEXCOORD2;
				float3 ray_direction : TEXCOORD3;
			};

			struct frag_out
			{
				half4 color : SV_Target0;
				float depth : SV_Depth;
			};
			
			v2f vert(appdata_base v)
			{
				v2f o;
				o.world_pos = mul(unity_ObjectToWorld, v.vertex).xyz;
				o.pos = mul(unity_MatrixVP, float4(o.world_pos, 1));
				o.screen_pos = ComputeScreenPos(o.pos);
				if(UNITY_MATRIX_P[3][3] == 1.f)
				{
					const float3 cam_dir = unity_CameraToWorld._m02_m12_m22;
					const float3 obj_scale = unity_ObjectToWorld._m00_m11_m22;
					o.ray_direction = mul(unity_WorldToObject, float4(cam_dir, 0));
					o.ray_origin = v.vertex - o.ray_direction * 2 * max(obj_scale);
				}
				else
				{
					o.ray_origin = mul(unity_WorldToObject, _WorldSpaceCameraPos);
					o.ray_direction = v.vertex - o.ray_origin;
				}
				
				return o;
			}
			
			frag_out frag(v2f i)
			{
				frag_out o;

				int x, y, z;
				_Volume.GetDimensions(x, y, z);
				if(x != 256 || y != 256)
				{
					// Invalid volume
					clip(-1);
					return o;
				}

				//const float2 frag_pos = i.screen_pos.xy / i.screen_pos.w;
				//float real_depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, frag_pos);
				//real_depth = Linear01Depth(real_depth) * (_ProjectionParams.z - _ProjectionParams.y) + _ProjectionParams.y;

				ray ray;
				ray.dir = i.ray_direction;
				ray.origin = i.ray_origin;
				// Not necessary. Will be overriden during raycast anyways.
				// ray.inv_dir = 1 / ray.dir
				
				float4 color;
				float3 voxel_obj_pos;
				float voxel_size;
				float3 hit_obj_pos;
				int attributes_ptr;
				float3 face_normal;
				int loops;
				if(cast_ray(ray, _Volume, color, voxel_obj_pos, voxel_size,
					hit_obj_pos, attributes_ptr, face_normal, loops))
				{
					// BEGIN LIGHTING CODE
					const float3 hit_world_pos = mul(unity_ObjectToWorld, hit_obj_pos);
					float3 normal = face_normal;
					normal = UnityObjectToWorldNormal(normal);
					const half light0_strength = max(0.25, dot(normal, _WorldSpaceLightPos0.xyz));

					const float3 world_view_dir = normalize(UnityWorldSpaceViewDir(hit_world_pos));
					const float3 world_refl = reflect(-world_view_dir, normal);
					const half3 spec = float3(1.f, 1.f, 1.f) * _Shininess * pow(max(0, dot(world_refl, _WorldSpaceLightPos0.xyz)), 32);
					
					o.color = color * light0_strength;
					o.color.rgb += ShadeSH3Order(half4(normal, 1));
					o.color.rgb += spec;
					// END LIGHTING CODE
					
					const float4 clip_pos = UnityWorldToClipPos(hit_world_pos);
					o.depth = clip_pos.z / clip_pos.w;
					#if defined(SHADER_API_GLCORE) || defined(SHADER_API_OPENGL) || \
						defined(SHADER_API_GLES) || defined(SHADER_API_GLES3)
						o.depth = (o.depth + 1.0) * 0.5;
					#endif
					
					return o;
				}
				clip(-1.f);
				return o;
			}

			
			ENDHLSL
		}
		
		Pass 
		{
			Tags {
				"LightMode" = "ShadowCaster"
			}
			CGPROGRAM

			#include "AutoLight.cginc"
			#include "UnityCG.cginc"
			#include "GeometryRayCast.hlsl"

			float _Shininess;
			Texture3D<int> _Volume;
			sampler2D _CameraDepthTexture;

			struct appdata 
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float2 uv     : TEXCOORD0;
			};

			struct v2f
			{
				V2F_SHADOW_CASTER;
				float3 world_pos : TEXCOORD0;
				float4 screen_pos : TEXCOORD1;
				float3 ray_origin : TEXCOORD2;
				float3 ray_direction : TEXCOORD3;
			};

			v2f Vert(appdata v)
			{
				v2f o;
				o.world_pos = mul(unity_ObjectToWorld, v.vertex).xyz;
				
				o.pos = UnityObjectToClipPos(v.vertex);
				#ifdef DISABLE_VIEW_CULLING
				o.pos.z = 1;
				#endif
				
				o.screen_pos = ComputeScreenPos(o.pos);
				if(UNITY_MATRIX_P[3][3] == 1.f)
				{
					const float3 cam_dir = unity_CameraToWorld._m02_m12_m22;
					const float3 obj_scale = unity_ObjectToWorld._m00_m11_m22;
					o.ray_direction = mul(unity_WorldToObject, float4(cam_dir, 0));
					o.ray_origin = v.vertex - o.ray_direction * 2 * max(obj_scale);
				}
				else
				{
					o.ray_origin = mul(unity_WorldToObject, _WorldSpaceCameraPos);
					o.ray_direction = v.vertex - o.ray_origin;
				}
				
				return o;
			}

			float4 Frag(v2f i) : SV_Target
			{
				ray r;
				r.dir = i.ray_direction;
				r.origin = i.ray_origin;
				
				float4 color;
				float3 voxel_obj_pos;
				float voxel_size;
				float3 hit_obj_pos;
				int attributes_ptr;
				float3 face_normal;
				int loops;
				if(cast_ray(r, _Volume, color, voxel_obj_pos, voxel_size,
					hit_obj_pos, attributes_ptr, face_normal, loops))
					discard;
				SHADOW_CASTER_FRAGMENT(i);
			}

			ENDCG
		}
	}
}