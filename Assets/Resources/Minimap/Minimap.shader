Shader "C,ustom/Minimap" {
	Properties{
		_Color ("Main Color", COLOR) = (1,1,1,0)
		_MainTex ("Main Texture", 2D) = "white"
	}
	SubShader {
		Pass{
			Lighting on
			Material{
				Diffuse[_ModelLightColor]
			}
			blend zero DstColor
			SetTexture[_MainTex]{
				combine primary
				Matrix [_Projector]
			}
		}
	} 
}
