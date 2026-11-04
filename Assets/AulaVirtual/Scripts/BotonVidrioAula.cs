using UnityEngine;
using UnityEngine.UI;

// Solo cambia parámetros de su Graphic; no toca el material compartido ni sus eventos.
public sealed class BotonVidrioAula : Button
{
    protected override void DoStateTransition(SelectionState state, bool instant)
    {
        base.DoStateTransition(state,instant);
        if (!(targetGraphic is VidrioBibliotecaGraphic glass)) return;
        bool focus = state == SelectionState.Highlighted || state == SelectionState.Selected;
        glass.colorBorde = state == SelectionState.Pressed ? new Color(.435f,.906f,.855f,.5f) :
            focus ? new Color(.663f,.847f,1,.48f) : new Color(1,1,1,.16f);
        glass.grosorBorde = focus ? 1.3f : .8f;
        glass.SetVerticesDirty();
    }
}
