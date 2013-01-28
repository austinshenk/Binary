#pragma strict

public class Resource extends StationaryNeutral{
	public var currentResources = new Array();
	public var resourcesAllowed = new Array();
	public var gatherAmount = new Array();
	
	function Start () {
		for(var i=0;i<resourcesAllowed.length;i++){
			if(currentResources[i] == null) currentResources.Push(0);
		}
		GameObject.FindGameObjectWithTag("GameManager").GetComponent(GameManager).addResourceStockpile(this);
	}

	function gatherResources(gathererNames:Array, gathererAmounts:Array){
		for(var i=0;i<resourcesAllowed.length;i++){
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
	static function combineResources(targetNames:Array, targetAmounts:Array, sourceNames:Array, sourceAmounts:Array, transferAmounts:Array){
		for(var i:int = 0;i<sourceNames.length;i++){
			for(var j:int=0;j<targetNames.length;j++){
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