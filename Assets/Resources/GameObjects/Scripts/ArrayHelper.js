#pragma strict
import System.Collections.Generic;

public static class ArrayHelper{
	function Pop(list:Object[], t:System.Type){
		subList(list, 0, list.Length-1);
	}
	function Push(list:Object[], obj:Object, t:System.Type){
		subList(list, 0, list.length+1);
		list[list.length-1] = obj;
	}
	function subList(list:Object[], start:int, length:int){
		var temp:Object[] = new Object[length];
		for(var i:int=start;i<length;i++){
			temp[i] = list[i];
		}
		list = temp;
	}
	function removeAt(list:Object[], index:int, t:System.Type){
		var temp:Object[] = new Object[list.length-1];
		for(var i:int=0;i<temp.length;i++){
			if(i != index) temp[i] = list[i];
		}
		list = temp;
	}
	function contains(list:Object[], obj:Object):int{
		for(var i:int=0;i<list.length;i++){
			if(list[i].Equals(obj)) return i;
		}
		return -1;
	}
	function addAt(list:Object[], obj:Object, index:int){
		var temp:Object[] = new Object[0];
		if(index > list.length-1){
			temp = new Object[index+1];
			for(var i:int=0;i<temp.length;i++){
				if(i == index) temp[i] = obj;
				else if(i < index && i < list.length) temp[i] = list[i];
				else temp[i] = null;
			}
		}
		else{
			temp = new Object[list.length];
			for( i=0;i<list.length;i++){
				if(i == index) temp[i] = obj;
				else if(i < index) temp[i] = list[i];
				else temp[i] = list[i+1];
			}
		}
		Debug.Log(temp.length);
		list = temp;
	}
}