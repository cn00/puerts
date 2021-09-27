import {$extension, $generic, $typeof} from 'puerts';
import {System, TsBehaviour, UnityEngine} from 'csharp';

export class #SCRIPTNAME#{
    public Awake() {console.log("ts Awake", this.gameObject);}
    public Start() { console.log("ts Start", this.gameObject); }
    // public Update(){console.log("ts Update", this.gameObject); }

    BindMono(mono:TsBehaviour){
        mono.JsAwake = ()=>this.Awake();
        mono.JsStart = ()=>this.Start();
        // mono.JsUpdate = ()=>this.Update();
    }
    // AutoGen Begin
    constructor(mono:TsBehaviour) {}
    // AutoGen End
    mono:TsBehaviour;
    gameObject:UnityEngine.GameObject;
}
(function () { 
    return function (mono:TsBehaviour):#SCRIPTNAME# {
        return new #SCRIPTNAME#(mono);
    }
})()
