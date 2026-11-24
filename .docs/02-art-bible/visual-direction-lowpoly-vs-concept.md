# Dirección de Arte: Estilo Visual Final vs. Concept Art

> [!IMPORTANT]
> **Directriz Crítica e Innegociable de Dirección de Arte:**
> Aunque las ilustraciones de paisajes y del enemigo en [`concept-art/`](concept-art/) presentan un acabado de renderizado realista o cinematográfico, **el videojuego en Unity 3D (`6000.6.0f1` URP) tendrá un estilo animado, estilizado / *low-poly* texturizado** (con proporciones caricaturescas, colores vibrantes y materiales limpios).
>
> Las imágenes conceptuales adjuntas deben documentarse y utilizarse exclusivamente como **referencias de composición, atmósfera, siluetas e iluminación** (contraste entre luces frías árticas y luces cálidas/neón industriales), **nunca como referencia de fotorrealismo**.

---

## 1. El Punto de Referencia Visual: El Protagonista Rediseñado

El concepto actualizado del **Duende Ingeniero / Minero** ([`concept-art/personajes/concepto_duende_minero.png`](concept-art/personajes/concepto_duende_minero.png)) establece el estándar estético al que deben adaptarse todos los entornos, estructuras y enemigos al ser modelados en 3D:

1. **Formas Limpias y Legibles (*Stylized / Low-Poly Texturizado*):** Volúmenes bien definidos, biseles marcados (*chamfers*) y ausencia de ruido fotográfico de alta frecuencia.
2. **Proporciones Caricaturescas (*Chibi* / Compactas):** Personajes y máquinas con siluetas amigables, robustas y expresivas, similares a referentes estilizados en 3D como *Astroneer*, *Slime Rancher* o *Deep Rock Galactic* con texturizado PBR estilizado limpio.
3. **Materiales Táctiles Simplificados:** El cuero del abrigo, el latón del tanque-mochila, el acero del casco y el brillo del cristal de caramelo rojo se distinguen por su respuesta especular y gradientes de color limpios, no por poros microscópicos realistas.

---

## 2. Tabla de Traducción: De Concept Art a Asset 3D en Unity

| Bioma / Entidad en Concept Art | Qué conservar del Concept Art | Cómo traducirlo al Estilo *Low-Poly* Estilizado en Unity 3D |
| :--- | :--- | :--- |
| **Aldea Industrial y Forja Central** | Silueta de las dos torres gemelas, el anillo circular incandescente de la forja y los pinos nevados laterales. | Bloques modulares de geometría limpia con remaches grandes, texturas de metal pintado estilizado y un shader emisivo naranja brillante con *Bloom* en el centro de la forja. |
| **Tundra, Bosque de Caramelos y Cementerio** | Las auroras boreales en el cielo, los árboles rojos y blancos ramificados y los trineos rojos semienterrados. | Pinos y árboles de caramelo formados por facetas poligonales estilizadas (*low-poly faceted/smooth hybrid*) con colores saturados y nieve suave sin micro-ruido. |
| **Túneles y Cavernas (Niveles 1 a 3)** | La estructura circular del túnel con tuberías y guirnaldas, los cristales azules facetados de la Geoda y la fisura naranja del Abismo. | Módulos de cueva y cristales geométricos de caras planas y aristas iluminadas (*edge highlight*), enfatizando el rebote de luz de las guirnaldas navideñas y el magma. |
| **Soldado de Juguete Defectuoso** | Cabeza esférica oxidada, ojos circulares brillantes en ámbar, boca dentada recortada y torso de placas remachadas. | Modelado con las mismas proporciones compactas y estilizadas del Duende Ingeniero (pintura desgastada tipo *cel/hand-painted* y chispas caricaturescas), evitando el terror fotorrealista. |
