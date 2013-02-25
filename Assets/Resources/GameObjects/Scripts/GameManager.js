#pragma strict
import System.Collections.Generic;

public class GameManager extends MonoBehaviour{
	public static var players:List.<RTSPlayer> = new List.<RTSPlayer>();
	public static var resourceStockpiles:List.<Resource> = new List.<Resource>();
	public static var resourceBuildings:List.<Building> = new List.<Building>();
	static function addResourceStockpile(obj:Resource){
		resourceStockpiles.Add(obj);
	}
	static function addResourceBuilding(obj:Building){
		resourceBuildings.Add(obj);
	}
	static function addPlayer(obj:RTSPlayer){
		players.Add(obj);
	}
}