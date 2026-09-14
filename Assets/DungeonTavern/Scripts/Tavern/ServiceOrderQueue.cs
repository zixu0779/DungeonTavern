using System.Collections.Generic;
using System.Linq;
using UnityEngine.AI;
using UnityEngine;

namespace DungeonTavern.Gameplay.Interaction
{
    public sealed class ServiceOrderQueue : MonoBehaviour
    {
        [SerializeField] private List<Transform> queuePoints = new();
        [SerializeField] private Transform menuAnchor;
        public Transform MenuAnchor => menuAnchor;
        public int Count { get { RemoveMissing(); return customers.Count; } }
        public CustomerServicePoint FirstCustomer { get { RemoveMissing(); return customers.Count == 0 ? null : customers[0]; } }
        public void BindMenu(Transform anchor) => menuAnchor = anchor;
        private readonly List<CustomerServicePoint> customers = new();
        private GroupOrderingSession session;
        private readonly Dictionary<CustomerServicePoint,Vector3> orderingPositions=new();
        private void Update()
        {
            if(session==null)return;
            session.Tick(Time.deltaTime);
            if(session.Finished){session=null;orderingPositions.Clear();}
        }
        public bool AtOrderingPosition(CustomerServicePoint guest) => orderingPositions.TryGetValue(guest,out var p)
            && Vector3.ProjectOnPlane(guest.transform.position-p,Vector3.up).sqrMagnitude<.3f*.3f;
        public void TryBeginGroup(CustomerServicePoint head)
        {
            if(session!=null||!IsFirst(head))return;
            var members=customers.Where(c=>c==head||head.PartyId!=0&&c.PartyId==head.PartyId).ToArray();
            Vector3 anchor=MenuAnchor.position,forward=Vector3.ProjectOnPlane(MenuAnchor.forward,Vector3.up).normalized;
            Vector3 right=Vector3.Cross(Vector3.up,forward);
            var positions=new List<Vector3>();
            // A shallow arc keeps everyone on the accessible side of the menu.
            for(int i=0;i<members.Length;i++)
            {
                float angle=members.Length==1?0:Mathf.Lerp(-65,65,i/(float)(members.Length-1))*Mathf.Deg2Rad;
                var desired=members.Length==1?anchor:anchor+forward*.55f+right*Mathf.Sin(angle)*1.7f-forward*Mathf.Cos(angle)*1.7f;
                if(!NavMesh.SamplePosition(desired,out var hit,.65f,NavMesh.AllAreas))return;
                var path=new NavMeshPath();
                if(!NavMesh.CalculatePath(members[i].transform.position,hit.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete
                    ||positions.Exists(p=>Vector3.Distance(p,hit.position)<.7f))return;
                positions.Add(hit.position);
            }
            for(int i=0;i<members.Length;i++)orderingPositions[members[i]]=positions[i];
            session=new GroupOrderingSession(members,this,FindAnyObjectByType<TavernMenuSystem>());
        }

        public void Configure(IEnumerable<Transform> points)
        {
            queuePoints.Clear();
            queuePoints.AddRange(points);
            queuePoints.RemoveAll(point => point == null);
        }

        public void Enqueue(CustomerServicePoint customer)
        {
            RemoveMissing();
            if (customer != null && !customers.Contains(customer))
                customers.Add(customer);
        }

        public void Remove(CustomerServicePoint customer) => customers.Remove(customer);

        public bool IsFirst(CustomerServicePoint customer)
        {
            RemoveMissing();
            return customers.Count > 0 && customers[0] == customer;
        }

        public Vector3 GetPosition(CustomerServicePoint customer)
        {
            RemoveMissing();
            if(orderingPositions.TryGetValue(customer,out var groupPosition))return groupPosition;
            queuePoints.RemoveAll(point => point == null);
            int index = Mathf.Max(0, customers.IndexOf(customer));
            Vector3 anchor = menuAnchor == null ? transform.position : menuAnchor.position;
            if (queuePoints.Count == 0)
                return anchor - (menuAnchor == null ? transform.forward : menuAnchor.forward) * index;
            int authored = Mathf.Min(index, queuePoints.Count - 1);
            // Queue markers are authored in world space; only the head is the menu anchor.
            // Never translate a second time when the menu or its anchor moves.
            Vector3 position = index == 0 ? anchor : queuePoints[authored].position;
            if (index >= queuePoints.Count)
            {
                Vector3 direction = queuePoints.Count > 1
                    ? (queuePoints[^1].position - queuePoints[^2].position).normalized
                    : -(menuAnchor == null ? transform.forward : menuAnchor.forward);
                if (direction.sqrMagnitude < .01f) direction = Vector3.back;
                position += direction * (index - queuePoints.Count + 1);
            }
            return position;
        }

        public Vector3 GetFacing(CustomerServicePoint customer)
        {
            if(orderingPositions.ContainsKey(customer))
            {
                var toward=menuAnchor.position+menuAnchor.forward*.8f-customer.transform.position;toward.y=0;
                return toward.sqrMagnitude>.001f?toward.normalized:menuAnchor.forward;
            }
            int index = customers.IndexOf(customer);
            Vector3 direction = index > 0 ? GetPosition(customers[index - 1]) - GetPosition(customer)
                : (menuAnchor == null ? transform.forward : menuAnchor.forward);
            direction.y = 0;
            return direction.sqrMagnitude > .001f ? direction.normalized : transform.forward;
        }

        private void RemoveMissing() => customers.RemoveAll(customer => customer == null);
    }
}
