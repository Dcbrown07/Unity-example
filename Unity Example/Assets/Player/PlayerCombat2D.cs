using UnityEngine;
using System.Collections;

public class PlayerCombat2D : MonoBehaviour
{
    [Header("Orb Casting")]
    public Transform orbSpawnPoint;
    public GameObject orbPrefab;

    [Header("Parry")]
    public float parryDuration = 0.3f;
    private bool isParrying = false;
    public GameObject parryIndicator;

    void Update()
    {
        HandleParryIndicator();

        if (Input.GetMouseButtonDown(0))
            CastOrbAtMouse();

        if (Input.GetMouseButtonDown(1))
            StartCoroutine(Parry());
    }

    void CastOrbAtMouse()
    {
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = (Vector2)(mouseWorld - orbSpawnPoint.position);

        GameObject orb = Instantiate(orbPrefab, orbSpawnPoint.position, Quaternion.identity);
        PongOrb orbScript = orb.GetComponent<PongOrb>();
        orbScript.owner = gameObject;
        orbScript.SetDirection(direction.normalized);

        Debug.Log("Player casted orb at mouse");
    }

    IEnumerator Parry()
    {
        isParrying = true;
        Debug.Log("Player started parry!");
        yield return new WaitForSeconds(parryDuration);
        isParrying = false;
        Debug.Log("Player parry ended!");
    }

    void HandleParryIndicator()
    {
        parryIndicator.SetActive(isParrying);
    }

    public bool IsParrying() => isParrying;
}