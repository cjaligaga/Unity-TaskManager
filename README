# TaskManager
A Futher implementation of Coroutines in Unity3D

This implementation is a derived implementation from 
https://github.com/krockot/Unity-TaskManager

Instead of these codes:

```C#
void Start() {
  StartCoroutine(Loop20Sec());
}

void OnDisable() {
  StopCoroutine(Loop20Sec());
}

IEnumerator Loop20Sec() {
  float tim = Time.time + 20;
		while (tim > Time.time) {
		  float progress = 1 - ((tim - Time.time) / totalTim);
		  //Moving from (0,0,0) to (1,0,0)
		  transform.position = new Vector3(progress,0,0);
  }
}
```

why don't just wrap up that coroutine to a single code?

```C#
Task c;
void Start() {
  //This make life just get more simpler to tweening things
  c = Task.Get(delegate(float t) { transform.position = new Vector3(t,0,0); }, 20);
}

void OnDisable() {
  c.Stop();
}
```
not just using delegates, this Task also support pause/resume system, And better handling for specific Task,
Which is a feature from the base code (https://github.com/krockot/Unity-TaskManager). Additionally, when it's 
got unused, it's resources (memory) will automatically kept for later usage, this implementation based from my
[TEXDraw](http://u3d.as/mFe) package. This also means that task will performs ss fast as possible, without 
suffer from GC Allocations.

See sources for more example and documentation.
