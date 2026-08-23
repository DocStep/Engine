using Newtonsoft.Json;

namespace Engine.Graphics.UI;

public class Canvas : Component, IUpdate {

    public override string Name => nameof(Canvas);

    //[Hide][JsonIgnore] public Matrix4x4 m4x4_View = Matrix4x4.Identity;
    //[Hide][JsonIgnore] public Matrix4x4 m4x4_Projection = Matrix4x4.Identity;



    public void Update () {
        CollectChildren(gameObject.Transform);
    }
    private void CollectChildren (Transform t) {
        if (!t.Enabled) return;

        t.gameObject.GetComponent<Image>()?.Submit();

        foreach (Transform child in t.Children)
            CollectChildren(child);
    }


    public GameObject? Pick (Vector2 mousePos) {
        return PickChildren(gameObject.Transform, mousePos);
    }

    private GameObject? PickChildren (Transform t, Vector2 mousePos) {

        /// reverse order: last-drawn (topmost) children get priority
        int count = t.Children.Count;
        for (int i = count-1; 0 <= i; i--) {
            Transform child = t.Children[i];
            if (!child.Enabled) continue;

            GameObject? hit = PickChildren(child, mousePos);
            if (hit is not null) return hit;

            Image? image = child.gameObject.GetComponent<Image>();
            if (image is null || !image.Enabled) continue;

            Vector3 pos = child.Position;
            bool inside =
                pos.X <= mousePos.X && mousePos.X <= pos.X + image.Size.X &&
                pos.Y <= mousePos.Y && mousePos.Y <= pos.Y + image.Size.Y;

            if (inside) return child.gameObject;
        }

        return null;
    }

}