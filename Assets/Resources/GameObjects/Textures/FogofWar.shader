Shader "Custom/FogofWar" {
	 Properties {
  	  _Color ("Main Color", Color) = (1,1,1,1)   	
  }
  Subshader {
     Pass {
     	lighting off
     	ZTest Greater
        Color [_Color]
     }
  }
}