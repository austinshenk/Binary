Shader "Custom/FogofWarSearched" {
	 Properties {
  	  _Color ("Main Color", Color) = (0,0,0,0)   	
  }
  Subshader {
     Pass {
     	lighting off
     	ZTest Greater
        Color [_Color]
     }
  }
}