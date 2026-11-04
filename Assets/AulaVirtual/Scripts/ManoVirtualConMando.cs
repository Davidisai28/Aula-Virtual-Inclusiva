using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

// Etapa 1: representación visual únicamente. No escribe poses, acciones ni interacciones.
public sealed class ManoVirtualConMando : MonoBehaviour
{
    public const string Id="ManoVirtual_Mando_v1";
    public bool izquierda;
    public Renderer[] modelosMando;
    public Renderer[] modelosMano;
    public enum Grupo { Pulgar, Indice, Agarre }
    [Serializable] public class Articulacion
    {
        public Transform hueso;
        public Grupo grupo;
        public Quaternion reposo;
        public Vector3 eje=Vector3.right;
        public float grados;
    }
    public Articulacion[] articulaciones;
    [Header("Calibración visual relativa al mando (no cambia su tracking)")]
    public Vector3 desplazamiento=new Vector3(0,-.015f,-.055f);
    public Vector3 orientacion;
    [Range(.75f,1.3f)] public float escala=1;
    [Range(1,30)] public float suavizado=14;
#if UNITY_EDITOR
    [Header("Prueba visual opcional en Play, sin cambiar acciones")]
    public bool probarPoseConInspector;
    [Range(0,1)] public float gatilloPrueba, agarrePrueba, pulgarPrueba;
#endif
    bool[] estadosOriginales;
    float indice, agarre, pulgar;
    void OnEnable()
    {
        estadosOriginales=new bool[modelosMando!=null?modelosMando.Length:0];
        for(int i=0;i<estadosOriginales.Length;i++) if(modelosMando[i]!=null) estadosOriginales[i]=modelosMando[i].forceRenderingOff;
        Visibilidad(false);
    }
    void LateUpdate()
    {
        var device=izquierda?UnityEngine.InputSystem.XR.XRController.leftHand:UnityEngine.InputSystem.XR.XRController.rightHand;
        bool tracked=device!=null && device.enabled && device.isTracked.ReadValue()>.5f;
        Visibilidad(tracked);
        if(!tracked) return;
        transform.localPosition=desplazamiento;
        transform.localRotation=Quaternion.Euler(orientacion);
        transform.localScale=Vector3.one*escala;
        float trigger=Valor(device,"trigger"), grip=Valor(device,"grip");
        float indexTarget=Mathf.Max(trigger,Valor(device,"triggerTouched")>.5f?.18f:0);
        bool thumbSensors=device.TryGetChildControl<ButtonControl>("thumbrestTouched")!=null || device.TryGetChildControl<ButtonControl>("primaryTouched")!=null;
        float thumbTarget=thumbSensors ? Mathf.Max(Valor(device,"thumbrestTouched"),Mathf.Max(Valor(device,"thumbstickTouched"),
            Mathf.Max(Valor(device,"primaryTouched"),Valor(device,"secondaryTouched"))))*.8f : Mathf.Max(grip*.65f,trigger*.35f);
#if UNITY_EDITOR
        if(probarPoseConInspector) {indexTarget=gatilloPrueba; grip=agarrePrueba; thumbTarget=pulgarPrueba;}
#endif
        float t=1-Mathf.Exp(-suavizado*Time.unscaledDeltaTime);
        indice=Mathf.Lerp(indice,indexTarget,t); agarre=Mathf.Lerp(agarre,grip,t); pulgar=Mathf.Lerp(pulgar,thumbTarget,t);
        if(articulaciones==null) return;
        foreach(var joint in articulaciones)
            if(joint.hueso!=null)
            {
                float amount=joint.grupo==Grupo.Pulgar?pulgar:joint.grupo==Grupo.Indice?indice:agarre;
                joint.hueso.localRotation=joint.reposo*Quaternion.AngleAxis(joint.grados*amount,joint.eje.normalized);
            }
    }
    static float Valor(InputDevice device,string name)
    {
        var control=device.TryGetChildControl<AxisControl>(name);
        return control!=null?Mathf.Clamp01(control.ReadValue()):0;
    }
    void Visibilidad(bool manos)
    {
        bool valid=false;
        if(modelosMano!=null) foreach(var r in modelosMano) if(r!=null && r.enabled && r.gameObject.activeInHierarchy) { valid=true; break; }
        manos &= valid;
        if(modelosMano!=null) foreach(var r in modelosMano) if(r!=null) r.forceRenderingOff=!manos;
        if(modelosMando!=null)
            for(int i=0;i<modelosMando.Length;i++) if(modelosMando[i]!=null)
                modelosMando[i].forceRenderingOff=manos || (estadosOriginales!=null && i<estadosOriginales.Length && estadosOriginales[i]);
    }
    void OnDisable()
    {
        Visibilidad(false);
        if(articulaciones!=null) foreach(var joint in articulaciones) if(joint.hueso!=null) joint.hueso.localRotation=joint.reposo;
    }
    [ContextMenu("Registrar entradas de la mano")]
    public void Registrar()
    {
        var d=izquierda?UnityEngine.InputSystem.XR.XRController.leftHand:UnityEngine.InputSystem.XR.XRController.rightHand;
        Debug.Log(d==null?"[MANOS-MANDO] No hay dispositivo para esta mano.":
            $"[MANOS-MANDO] {d.layout}, tracked={d.isTracked.ReadValue()}, trigger={Valor(d,"trigger"):F2}, grip={Valor(d,"grip"):F2}, triggerTouched={Valor(d,"triggerTouched"):F0}, thumbrestTouched={Valor(d,"thumbrestTouched"):F0}. Poses visuales, no Capsense SDK ni hand tracking.",this);
    }
}
