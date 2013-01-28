Shader "Custom/MapItem" {
  Properties {
  	  _Color ("Main Color", Color) = (1,1,1,1)   	
  }
  Subshader {
     Pass {
     	Lighting off
        Color [_Color]
     }
  }
}
