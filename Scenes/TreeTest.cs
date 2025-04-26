using Godot;
using System;

public partial class TreeTest : Tree
{
    public override void _Ready()
    {
        var tree = this;
        TreeItem root = tree.CreateItem();
        tree.HideRoot = true;
        TreeItem child1 = tree.CreateItem(root);
        TreeItem child2 = tree.CreateItem(root);
        TreeItem subchild1 = tree.CreateItem(child1);
        subchild1.SetText(0, "Subchild1");
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }
}
