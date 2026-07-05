namespace Engine;


[Serializable]
public class Toggle_PostProcessing : Toggle {
    public Toggle_PostProcessing () : base("PostProcessing", isOn: true, layout: Layout.Right) {
        //Material[] materials = Resources.LoadAll<Material>("Materials");
        //int count = materials.Length;
        //for (int i = 0; i < count; i++) {
        //    this.materials.Add(materials[i]);
        //}

        //Debug.Log($"Toggle_PostProcessing");
    }

    [Newtonsoft.Json.JsonIgnore] List<Graphics.Material> materials = new List<Graphics.Material>();


    public override void Apply () {
        int count = materials.Count;
        if (isOn) {
            for (int i = 0; i < count; i++) {
                //materials[i].SetFloat("uPostProcessing", 1);
            }
        } else {
            for (int i = 0; i < count; i++) {
                //materials[i].SetFloat("uPostProcessing", 0);
            }
        }
    }


    public override void Link () {
        SettingsEngine.Instance.GraphicsEngine?.postProcessing = this;
    }
}
