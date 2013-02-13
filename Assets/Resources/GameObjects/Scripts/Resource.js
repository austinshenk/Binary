#pragma strict
import System.Collections.Generic;

public class Resource extends StationaryNeutral{
	public var currentResources:List.<int> = new List.<int>();
	public var resourcesAllowed:List.<String> = new List.<String>();
	public var gatherAmount:List.<int> = new List.<int>();
	
	function Start () {
		for(var i=0;i<resourcesAllowed.Count;i++){
			if(currentResources[i] == null) currentResources.Add(0);
		}
		GameObject.FindGameObjectWithTag("GameManager").GetComponent(GameManager).addResourceStockpile(this);
	}

	function gatherResources(gathererNames:List.<String>, gathererAmounts:List.<int>){
		for(var i=0;i<resourcesAllowed.Count;i++){
			var amount:int = currentResources[i];
			var deduct:int = gatherAmount[i];
			if(amount-deduct <= 0){
				Kill();
			}
			else{
				currentResources[i] = amount-deduct;
			}
		}
		Resource.combineResources(gathererNames, gathererAmounts, resourcesAllowed, currentResources, gatherAmount);
	}
	static function combineResources(targetNames:List.<String>, targetAmounts:List.<int>, sourceNames:List.<String>, sourceAmounts:List.<int>, transferAmounts:List.<int>){
		for(var i:int = 0;i<sourceNames.Count;i++){
			for(var j:int=0;j<targetNames.Count;j++){
				if(sourceNames[i].Equals(targetNames[j])){
					var amount1:int = sourceAmounts[i];
					var amount2:int = transferAmounts[i];
					sourceAmounts[i] = amount1 - amount2;
					amount1 = targetAmounts[i];
					targetAmounts[i] = amount1 + amount2;
				}
			}
		}
	}
}