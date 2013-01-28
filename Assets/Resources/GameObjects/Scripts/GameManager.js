#pragma strict

public static var players = new Array();
public var resourceStockpiles = new Array();
public var resourceBuildings = new Array();

public class GameManager extends MonoBehaviour{
	function addResourceStockpile(obj:Resource){
		resourceStockpiles.Add(obj);
	}
	function addResourceBuilding(obj:Building){
		resourceBuildings.Add(obj);
	}
	static function addPlayer(obj:RTSPlayer){
		players.Add(obj);
	}
}