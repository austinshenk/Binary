#pragma strict

public class Minimap extends MonoBehaviour{
	function Start(){
		setSize(150,150);
	}
	function setSize(width:int, height:int){
		camera.pixelRect = Rect(0,0,width,height);
	}
	static function addToMinimap(target:GameObject){
		var mapBounds = GameObject.CreatePrimitive(PrimitiveType.Cube);
		var material:Material = Resources.Load("Game_Objects/Materials/MapItem") as Material;
		mapBounds.name = "MapBounds";
		mapBounds.layer = LayerConstants.MINIMAP;
		Destroy(mapBounds.GetComponent(Collider));
		mapBounds.transform.localScale = target.collider.bounds.size;
		mapBounds.transform.parent = target.transform;
		mapBounds.transform.localPosition = Vector3(0,0,0);
		mapBounds.renderer.material = material;
		mapBounds.renderer.material.color = target.renderer.material.color;
	}
}