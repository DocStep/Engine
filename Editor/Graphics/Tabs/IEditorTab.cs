namespace Editor.Graphics;


public interface IEditorTab {

    string Name { get; set; }
    bool isActive { get; set; }

    //void SetActive () => isActive = true;
    //void SetInactive () => isActive = false;

    void Draw ();

}
