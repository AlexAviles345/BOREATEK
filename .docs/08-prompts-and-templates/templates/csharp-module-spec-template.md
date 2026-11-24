# Plantilla: Especificación Técnica de Módulo C#

```markdown
# Módulo M-XX: [Nombre del Módulo] (`NombreScript.cs`)

* **Namespace:** `Boreatek.<Subsistema>`
* **Ruta destino en Unity:** `Assets/_Project/Scripts/<Subsistema>/NombreScript.cs`
* **Componentes Requeridos:** `[RequireComponent(typeof(...))]`

---

## 1. Responsabilidades del Módulo
1. [Responsabilidad principal 1]
2. [Responsabilidad principal 2]

---

## 2. Contrato de Clase (Campos, Propiedades y Eventos)

\`\`\`csharp
namespace Boreatek.Subsistema
{
    public class NombreScript : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _exampleValue = 1f;

        public float ExampleValue => _exampleValue;

        public event Action<float> OnExampleChanged;
    }
}
\`\`\`
```
