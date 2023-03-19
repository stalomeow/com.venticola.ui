using UnityEngine;

namespace VentiCola.UI.Bindings
{
    public struct TemplateObject
    {
        private GameObject m_Obj;
        private bool m_SingleInstance;

        public TemplateObject(GameObject obj, bool singleInstance)
        {
            m_Obj = obj;
            m_SingleInstance = singleInstance;

            m_Obj.SetActive(singleInstance);
        }

        public GameObject RawGameObject
        {
            get => m_Obj;
        }

        public T GetRawAuthoringAs<T>() where T : LayoutBindingAuthoring
        {
            return m_Obj.GetComponent<T>();
        }

        public TemplateInstance Instantiate()
        {
            GameObject go;

            if (m_SingleInstance)
            {
                go = m_Obj;
            }
            else
            {
                go = Object.Instantiate(m_Obj, m_Obj.transform.parent, false);
                go.SetActive(true);
            }

            var authoring = go.GetComponent<LayoutBindingAuthoring>();
            return new TemplateInstance(authoring);
        }

        public void Dispose()
        {
            m_Obj.SetActive(true);
            m_Obj = null;
        }
    }
}
