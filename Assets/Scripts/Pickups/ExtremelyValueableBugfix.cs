using UnityEngine;

public class ExtremelyValueableBugfix : MonoBehaviour
{
    bool hasBeenFixed = false;
    [SerializeField] GameObject foo;
    [SerializeField] int bar;
    [SerializeField] bool superImportant = false;
    int roo;

    void Start()
    {
        print(name);
        if (foo == null) return;
        foo.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Thrown"))
        {
            roo++;
            if (!hasBeenFixed && bar == roo)
            {
                if(superImportant)
                {
                    foreach (var fix in PlayerActions.Instance.importantFix)
                    {
                        fix.SetActive(true);
                    }
                    PlayerActions.Instance.minorFix.SetActive(false);
                }
                if(foo != null)
                {
                    foo.SetActive(true);
                }
                hasBeenFixed = true;
            }
        }
    }
}
