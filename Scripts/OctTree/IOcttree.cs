using System.Collections.Generic;
using Godot;
public interface IOctree
{
    void Clear();
    List<int> Search(Vector3 position, float range);
    void ToggleDebug();
    void Insert(List<int> elements);
    Vector3 WrapPosition(Vector3 Position);
}
