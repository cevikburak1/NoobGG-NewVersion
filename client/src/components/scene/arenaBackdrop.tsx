import { useEffect, useRef } from 'react';
import * as THREE from 'three';

type SceneMode = 'match' | 'guild' | 'tournament' | 'social';

function modeForPath(pathname: string): SceneMode {
  if (pathname.startsWith('/rooms') || pathname.startsWith('/discover')) return 'match';
  if (pathname.startsWith('/guilds') || pathname.startsWith('/community')) return 'guild';
  if (pathname.startsWith('/tournaments') || pathname.startsWith('/leaderboard')) return 'tournament';
  return 'social';
}

export function ArenaBackdrop({ pathname }: { pathname: string }) {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const fallbackRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    canvas.style.display = '';
    fallbackRef.current?.classList.add('hidden');
    const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    canvas.dataset.motion = reducedMotion ? 'paused' : 'running';
    const lowPower = (navigator.hardwareConcurrency ?? 8) <= 4 || window.innerWidth < 640;
    let renderer: THREE.WebGLRenderer;
    try {
      renderer = new THREE.WebGLRenderer({ canvas, alpha: true, antialias: !lowPower, powerPreference: 'low-power' });
    } catch {
      canvas.style.display = 'none';
      fallbackRef.current?.classList.remove('hidden');
      return;
    }

    renderer.setPixelRatio(Math.min(window.devicePixelRatio, lowPower ? 1 : 1.5));
    const scene = new THREE.Scene();
    const camera = new THREE.PerspectiveCamera(45, 1, 0.1, 100);
    camera.position.set(0, 0, 15);
    scene.add(new THREE.AmbientLight(0xffffff, 1.15));
    const light = new THREE.PointLight(0x70f0d2, 32, 30);
    light.position.set(4, 5, 7);
    scene.add(light);
    const group = new THREE.Group();
    scene.add(group);

    const mode = modeForPath(pathname);
    const materials = [
      new THREE.MeshStandardMaterial({ color: 0x20d6bd, metalness: 0.58, roughness: 0.28, transparent: true, opacity: 0.78 }),
      new THREE.MeshStandardMaterial({ color: 0xff6e68, metalness: 0.5, roughness: 0.32, transparent: true, opacity: 0.72 }),
      new THREE.MeshStandardMaterial({ color: 0xffd166, metalness: 0.52, roughness: 0.3, transparent: true, opacity: 0.74 }),
    ];
    const shapes: THREE.Mesh[] = [];
    const addShape = (geometry: THREE.BufferGeometry, x: number, y: number, z: number, scale: number, color: number) => {
      const mesh = new THREE.Mesh(geometry, materials[color]);
      mesh.position.set(x, y, z);
      mesh.scale.setScalar(scale);
      group.add(mesh);
      shapes.push(mesh);
    };
    const nodes = mode === 'tournament'
      ? [[-4, 2], [-4, 0], [-4, -2], [-1, 1], [-1, -1], [2, 0], [4, 0]]
      : mode === 'guild'
        ? [[-3, 2], [0, 2.8], [3, 2], [-3.5, -1], [0, 0], [3.5, -1], [0, -3]]
        : [[-3.7, 1.6], [-1.2, 2.7], [1.7, 2], [4, 0.8], [-3, -1.4], [-0.4, -0.5], [2.3, -2.2], [0, -3]];
    const geometry = new THREE.IcosahedronGeometry(1, lowPower ? 0 : 1);
    nodes.forEach(([x, y], index) => addShape(geometry, x, y, (index % 3) - 1, index === 4 ? 0.85 : 0.38 + (index % 2) * 0.15, index % 3));
    if (mode === 'guild') {
      addShape(new THREE.TorusGeometry(2.4, 0.045, 8, 72), 0, 0, -1.6, 1, 2);
    } else if (mode === 'social') {
      addShape(new THREE.TorusKnotGeometry(1.1, 0.15, 80, 8), 0, 0, -1.8, 1, 0);
    }
    const links: number[] = [];
    const pairs = mode === 'tournament'
      ? [[0, 3], [1, 3], [2, 4], [3, 5], [4, 5], [5, 6]]
      : [[0, 1], [1, 2], [2, 3], [0, 4], [4, 5], [5, 6], [6, 7], [1, 5], [2, 5]];
    for (const [from, to] of pairs) {
      if (!nodes[from] || !nodes[to]) continue;
      links.push(nodes[from][0], nodes[from][1], -2, nodes[to][0], nodes[to][1], -2);
    }
    const lineGeometry = new THREE.BufferGeometry();
    lineGeometry.setAttribute('position', new THREE.Float32BufferAttribute(links, 3));
    group.add(new THREE.LineSegments(lineGeometry, new THREE.LineBasicMaterial({ color: 0x7ce8df, transparent: true, opacity: 0.35 })));

    let frame = 0;
    let pointerX = 0;
    let pointerY = 0;
    const onPointer = (event: PointerEvent) => {
      pointerX = event.clientX / window.innerWidth - 0.5;
      pointerY = event.clientY / window.innerHeight - 0.5;
    };
    const resize = () => {
      const width = window.innerWidth;
      const height = window.innerHeight;
      renderer.setSize(width, height, false);
      camera.aspect = width / height;
      camera.position.z = width < 640 ? 19 : 15;
      camera.updateProjectionMatrix();
      renderer.render(scene, camera);
    };
    const animate = () => {
      group.rotation.y += 0.0018;
      group.rotation.x += (pointerY * 0.12 - group.rotation.x) * 0.02;
      group.rotation.z += (pointerX * 0.08 - group.rotation.z) * 0.02;
      shapes.forEach((shape, index) => { shape.rotation.y += 0.002 + index * 0.0003; });
      renderer.render(scene, camera);
      frame = requestAnimationFrame(animate);
    };
    window.addEventListener('resize', resize);
    if (!reducedMotion) window.addEventListener('pointermove', onPointer);
    resize();
    if (!reducedMotion) animate();
    return () => {
      cancelAnimationFrame(frame);
      window.removeEventListener('resize', resize);
      window.removeEventListener('pointermove', onPointer);
      scene.traverse((object) => {
        if (object instanceof THREE.Mesh || object instanceof THREE.LineSegments) {
          object.geometry.dispose();
          const objectMaterials = Array.isArray(object.material) ? object.material : [object.material];
          objectMaterials.forEach((material) => material.dispose());
        }
      });
      renderer.dispose();
    };
  }, [pathname]);

  return (
    <>
      <div ref={fallbackRef} data-testid="scene-fallback" aria-hidden="true" className="pointer-events-none fixed inset-0 hidden overflow-hidden opacity-30">
        <div className="absolute left-[22%] top-[25%] h-40 w-40 rotate-45 border border-primary/50" />
        <div className="absolute right-[18%] top-[35%] h-28 w-28 rotate-12 border border-accent/60" />
        <div className="absolute bottom-[15%] left-[48%] h-36 w-36 rotate-45 border border-warning/50" />
      </div>
      <canvas ref={canvasRef} aria-hidden="true" className={`pointer-events-none fixed inset-0 h-screen w-screen ${pathname === '/' ? 'opacity-65' : 'opacity-25'}`} />
    </>
  );
}
