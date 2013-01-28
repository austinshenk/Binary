#pragma strict

public class MinimapItem extends MonoBehaviour{
	function Start(){
		Minimap.addToMinimap(this.gameObject);
	}
}