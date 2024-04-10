public class Parent
{
	public List<Child> children = new List<Child>();

	public Parent()
	{
		children.Add(new Child(1));
		children.Add(new Child(2));
		children.Add(new Child(3));
	}

	public void replace()
	{
		List<Child> newChildren = new List<Child>();
		newChildren.Add(new Child(4));
		newChildren.Add(new Child(5));
		children = newChildren;
	}
}

public class Child {
	public int childID;

	public Child(int id)
	{
		childID = id;
	}
}
