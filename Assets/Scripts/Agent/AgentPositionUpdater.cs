using UnityEngine;
using System.Collections;

public class AgentPositionUpdater : MonoBehaviour
{
    private Transform agentTransform;
    private PathDecisionManager decisionManager;

    private void Start()
    {
        agentTransform = transform;
        decisionManager = GetComponent<PathDecisionManager>();
        StartCoroutine(UpdateRoutine());
    }

    private IEnumerator UpdateRoutine()
    {
        while (true)
        {
            decisionManager.UpdateAgentPosition(agentTransform.position);
            yield return new WaitForSeconds(60f);
        }
    }
}
