/**
 * @license
 * SPDX-License-Identifier: Apache-2.0
 */

import React, { useEffect, useRef, useState } from 'react';
import { motion, AnimatePresence } from 'motion/react';
import { Play, RotateCcw, Zap } from 'lucide-react';

// --- Constants ---
const CANVAS_WIDTH = 800;
const CANVAS_HEIGHT = 600;
const PLAYER_RADIUS = 10;
const ENEMY_RADIUS = 12;
const PLAYER_SPEED = 4;
const REPLAY_SPEED_MULTIPLIER = 1.8;
const MAX_TIME_GAUGE = 100;
const GAUGE_CONSUMPTION_RATE = 0.5;
const GAUGE_RECOVERY_RATE = 0.2;
const TRAIL_LIFETIME = 3000; // ms
const ENEMY_SPAWN_INTERVAL = 2000;

// --- Types ---
type Point = { x: number; y: number; t: number };
type GameState = 'START' | 'PLAYING' | 'GAMEOVER';
type Phase = 'REALTIME' | 'PLANNING' | 'REPLAY';

interface Enemy {
  id: number;
  x: number;
  y: number;
  vx: number;
  vy: number;
  active: boolean;
}

interface TrailSegment {
  p1: Point;
  p2: Point;
  createdAt: number;
}

export default function App() {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const [gameState, setGameState] = useState<GameState>('START');
  const [score, setScore] = useState(0);
  const [highScore, setHighScore] = useState(0);
  const [timeGauge, setTimeGauge] = useState(MAX_TIME_GAUGE);
  const [phase, setPhase] = useState<Phase>('REALTIME');

  // Game Refs
  const playerRef = useRef({ x: CANVAS_WIDTH / 2, y: CANVAS_HEIGHT / 2, angle: 0 });
  const enemiesRef = useRef<Enemy[]>([]);
  const recordedPathRef = useRef<Point[]>([]);
  const trailsRef = useRef<TrailSegment[]>([]);
  const keysRef = useRef<{ [key: string]: boolean }>({});
  const lastTimeRef = useRef<number>(0);
  const enemyIdCounter = useRef(0);
  const lastEnemySpawnTime = useRef(0);
  const replayIndexRef = useRef(0);
  const replayStartTimeRef = useRef(0);
  const [captureFlash, setCaptureFlash] = useState<{ polygon: Point[]; time: number } | null>(null);

  // --- Initialization ---
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      keysRef.current[e.code] = true;
      if (e.code === 'Space' && gameState === 'PLAYING' && phase === 'REALTIME' && timeGauge > 10) {
        startPlanning();
      }
    };
    const handleKeyUp = (e: KeyboardEvent) => {
      keysRef.current[e.code] = false;
      if (e.code === 'Space' && phase === 'PLANNING') {
        startReplay();
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    window.addEventListener('keyup', handleKeyUp);
    return () => {
      window.removeEventListener('keydown', handleKeyDown);
      window.removeEventListener('keyup', handleKeyUp);
    };
  }, [gameState, phase, timeGauge]);

  const startPlanning = () => {
    setPhase('PLANNING');
    recordedPathRef.current = [{ 
      x: playerRef.current.x, 
      y: playerRef.current.y, 
      t: performance.now() 
    }];
  };

  const startReplay = () => {
    if (recordedPathRef.current.length < 2) {
      setPhase('REALTIME');
      return;
    }
    setPhase('REPLAY');
    replayIndexRef.current = 0;
    replayStartTimeRef.current = performance.now();
  };

  const resetGame = () => {
    playerRef.current = { x: CANVAS_WIDTH / 2, y: CANVAS_HEIGHT / 2, angle: 0 };
    enemiesRef.current = [];
    recordedPathRef.current = [];
    trailsRef.current = [];
    setScore(0);
    setTimeGauge(MAX_TIME_GAUGE);
    setPhase('REALTIME');
    setGameState('PLAYING');
    lastTimeRef.current = performance.now();
    lastEnemySpawnTime.current = performance.now();
  };

  // --- Game Loop ---
  useEffect(() => {
    if (gameState !== 'PLAYING') return;

    let animationFrameId: number;

    const loop = (time: number) => {
      const dt = time - lastTimeRef.current;
      lastTimeRef.current = time;

      update(time, dt);
      draw();

      animationFrameId = requestAnimationFrame(loop);
    };

    animationFrameId = requestAnimationFrame(loop);
    return () => cancelAnimationFrame(animationFrameId);
  }, [gameState, phase]);

  // Helper: Point in Polygon
  const isPointInPolygon = (px: number, py: number, polygon: Point[]) => {
    let inside = false;
    for (let i = 0, j = polygon.length - 1; i < polygon.length; j = i++) {
      const xi = polygon[i].x, yi = polygon[i].y;
      const xj = polygon[j].x, yj = polygon[j].y;
      const intersect = ((yi > py) !== (yj > py)) &&
        (px < (xj - xi) * (py - yi) / (yj - yi) + xi);
      if (intersect) inside = !inside;
    }
    return inside;
  };

  const checkTerritoryCapture = (path: Point[]) => {
    if (path.length < 10) return; // Need a significant path

    const start = path[0];
    const end = path[path.length - 1];
    const dist = Math.sqrt((start.x - end.x) ** 2 + (start.y - end.y) ** 2);

    // If start and end are close, it's a loop
    if (dist < 80) {
      let capturedCount = 0;
      enemiesRef.current.forEach(enemy => {
        if (enemy.active && isPointInPolygon(enemy.x, enemy.y, path)) {
          enemy.active = false;
          capturedCount++;
        }
      });

      if (capturedCount > 0) {
        setScore(s => s + (capturedCount * 200)); // Bonus for capture
        setCaptureFlash({ polygon: [...path], time: performance.now() });
      }
    }
  };

  const update = (time: number, dt: number) => {
    // 1. Handle Phase Transitions & Gauge
    if (phase === 'PLANNING') {
      setTimeGauge(prev => {
        const next = prev - GAUGE_CONSUMPTION_RATE;
        if (next <= 0) {
          setTimeout(() => startReplay(), 0);
          return 0;
        }
        return next;
      });
    } else if (phase === 'REALTIME') {
      setTimeGauge(prev => Math.min(MAX_TIME_GAUGE, prev + GAUGE_RECOVERY_RATE));
    }

    // 2. Player Movement
    if (phase === 'REALTIME' || phase === 'PLANNING') {
      let dx = 0;
      let dy = 0;
      if (keysRef.current['ArrowUp'] || keysRef.current['KeyW']) dy -= 1;
      if (keysRef.current['ArrowDown'] || keysRef.current['KeyS']) dy += 1;
      if (keysRef.current['ArrowLeft'] || keysRef.current['KeyA']) dx -= 1;
      if (keysRef.current['ArrowRight'] || keysRef.current['KeyD']) dx += 1;

      if (dx !== 0 || dy !== 0) {
        const mag = Math.sqrt(dx * dx + dy * dy);
        const speed = phase === 'PLANNING' ? PLAYER_SPEED * 0.7 : PLAYER_SPEED;
        playerRef.current.x += (dx / mag) * speed;
        playerRef.current.y += (dy / mag) * speed;
        playerRef.current.angle = Math.atan2(dy, dx);

        // Boundary check
        playerRef.current.x = Math.max(PLAYER_RADIUS, Math.min(CANVAS_WIDTH - PLAYER_RADIUS, playerRef.current.x));
        playerRef.current.y = Math.max(PLAYER_RADIUS, Math.min(CANVAS_HEIGHT - PLAYER_RADIUS, playerRef.current.y));

        if (phase === 'PLANNING') {
          recordedPathRef.current.push({ x: playerRef.current.x, y: playerRef.current.y, t: time });
        }
      }
    } else if (phase === 'REPLAY') {
      const path = recordedPathRef.current;
      if (replayIndexRef.current < path.length - 1) {
        // Move to next point in path
        // We use a simplified replay that just steps through the recorded points faster
        const stepsPerFrame = Math.ceil(REPLAY_SPEED_MULTIPLIER);
        for (let i = 0; i < stepsPerFrame && replayIndexRef.current < path.length - 1; i++) {
          const p1 = path[replayIndexRef.current];
          const p2 = path[replayIndexRef.current + 1];
          
          playerRef.current.x = p2.x;
          playerRef.current.y = p2.y;
          playerRef.current.angle = Math.atan2(p2.y - p1.y, p2.x - p1.x);

          // Add trail segment
          trailsRef.current.push({ p1, p2, createdAt: time });
          replayIndexRef.current++;
        }
      } else {
        checkTerritoryCapture(path);
        setPhase('REALTIME');
        recordedPathRef.current = [];
      }
    }

    // 3. Enemies
    if (phase !== 'PLANNING') {
      // Spawn enemies
      if (time - lastEnemySpawnTime.current > ENEMY_SPAWN_INTERVAL) {
        spawnEnemy();
        lastEnemySpawnTime.current = time;
      }

      // Update enemies
      enemiesRef.current.forEach(enemy => {
        if (!enemy.active) return;
        
        // Simple tracking AI
        const dx = playerRef.current.x - enemy.x;
        const dy = playerRef.current.y - enemy.y;
        const dist = Math.sqrt(dx * dx + dy * dy);
        
        const enemySpeed = 1.5 + (score / 1000);
        enemy.vx = (dx / dist) * enemySpeed;
        enemy.vy = (dy / dist) * enemySpeed;

        enemy.x += enemy.vx;
        enemy.y += enemy.vy;

        // Collision with player
        if (phase !== 'REPLAY') {
          if (dist < PLAYER_RADIUS + ENEMY_RADIUS) {
            endGame();
          }
        }

        // Collision with trails
        trailsRef.current.forEach(trail => {
          if (circleLineIntersect(enemy.x, enemy.y, ENEMY_RADIUS, trail.p1.x, trail.p1.y, trail.p2.x, trail.p2.y)) {
            enemy.active = false;
            setScore(s => s + 100);
          }
        });
      });

      // Cleanup inactive enemies
      enemiesRef.current = enemiesRef.current.filter(e => e.active);
    }

    // 4. Trails Cleanup
    if (phase === 'REALTIME') {
      // Clear capture flash after some time
      if (captureFlash && time - captureFlash.time > 500) {
        setCaptureFlash(null);
      }

      // Sequentially remove trails from the start right after replay ends
      if (trailsRef.current.length > 0) {
        // Remove segments per frame for a smooth sequential disappearance
        // We use a rate that feels natural but clears the path eventually
        const removalRate = Math.max(1, Math.ceil(trailsRef.current.length / 60)); 
        trailsRef.current.splice(0, removalRate);
      }
    }

    // 5. Player collision with own trails (only if not in replay or planning)
    if (phase === 'REALTIME') {
      trailsRef.current.forEach(trail => {
        // Skip very recent trails to avoid self-collision at the tail
        if (time - trail.createdAt > 500) {
          if (circleLineIntersect(playerRef.current.x, playerRef.current.y, PLAYER_RADIUS - 2, trail.p1.x, trail.p1.y, trail.p2.x, trail.p2.y)) {
            endGame();
          }
        }
      });
    }
  };

  const spawnEnemy = () => {
    const side = Math.floor(Math.random() * 4);
    let x, y;
    if (side === 0) { x = Math.random() * CANVAS_WIDTH; y = -ENEMY_RADIUS; }
    else if (side === 1) { x = CANVAS_WIDTH + ENEMY_RADIUS; y = Math.random() * CANVAS_HEIGHT; }
    else if (side === 2) { x = Math.random() * CANVAS_WIDTH; y = CANVAS_HEIGHT + ENEMY_RADIUS; }
    else { x = -ENEMY_RADIUS; y = Math.random() * CANVAS_HEIGHT; }

    enemiesRef.current.push({
      id: enemyIdCounter.current++,
      x, y, vx: 0, vy: 0, active: true
    });
  };

  const endGame = () => {
    setGameState('GAMEOVER');
    if (score > highScore) setHighScore(score);
  };

  const draw = () => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    // Background
    ctx.fillStyle = '#050505';
    ctx.fillRect(0, 0, CANVAS_WIDTH, CANVAS_HEIGHT);

    // Planning Phase Tint
    if (phase === 'PLANNING') {
      ctx.fillStyle = 'rgba(0, 100, 255, 0.1)';
      ctx.fillRect(0, 0, CANVAS_WIDTH, CANVAS_HEIGHT);
      
      // Noise effect
      for (let i = 0; i < 100; i++) {
        ctx.fillStyle = `rgba(255, 255, 255, ${Math.random() * 0.05})`;
        ctx.fillRect(Math.random() * CANVAS_WIDTH, Math.random() * CANVAS_HEIGHT, 2, 2);
      }
    }

    // Draw Recorded Path (Planning)
    if (phase === 'PLANNING' || phase === 'REPLAY') {
      ctx.setLineDash([5, 5]);
      ctx.strokeStyle = 'rgba(0, 255, 255, 0.5)';
      ctx.lineWidth = 2;
      ctx.beginPath();
      recordedPathRef.current.forEach((p, i) => {
        if (i === 0) ctx.moveTo(p.x, p.y);
        else ctx.lineTo(p.x, p.y);
      });
      ctx.stroke();
      ctx.setLineDash([]);
    }

    // Draw Trails
    ctx.shadowBlur = 15;
    
    // Draw Capture Flash
    if (captureFlash) {
      const age = performance.now() - captureFlash.time;
      const opacity = Math.max(0, 0.3 * (1 - age / 500));
      ctx.fillStyle = `rgba(0, 255, 255, ${opacity})`;
      ctx.beginPath();
      captureFlash.polygon.forEach((p, i) => {
        if (i === 0) ctx.moveTo(p.x, p.y);
        else ctx.lineTo(p.x, p.y);
      });
      ctx.closePath();
      ctx.fill();
    }

    trailsRef.current.forEach(trail => {
      const age = performance.now() - trail.createdAt;
      const opacity = 1 - (age / TRAIL_LIFETIME);
      ctx.strokeStyle = `rgba(0, 255, 255, ${opacity})`;
      ctx.shadowColor = 'cyan';
      ctx.lineWidth = 4;
      ctx.beginPath();
      ctx.moveTo(trail.p1.x, trail.p1.y);
      ctx.lineTo(trail.p2.x, trail.p2.y);
      ctx.stroke();
    });
    ctx.shadowBlur = 0;

    // Draw Enemies
    enemiesRef.current.forEach(enemy => {
      ctx.fillStyle = '#ff0044';
      ctx.shadowBlur = 10;
      ctx.shadowColor = '#ff0044';
      ctx.beginPath();
      ctx.arc(enemy.x, enemy.y, ENEMY_RADIUS, 0, Math.PI * 2);
      ctx.fill();
      
      // Enemy "eye" or core
      ctx.fillStyle = 'white';
      ctx.beginPath();
      ctx.arc(enemy.x, enemy.y, 4, 0, Math.PI * 2);
      ctx.fill();
    });
    ctx.shadowBlur = 0;

    // Draw Player
    ctx.save();
    ctx.translate(playerRef.current.x, playerRef.current.y);
    ctx.rotate(playerRef.current.angle);
    
    // Player Glow
    ctx.shadowBlur = 20;
    ctx.shadowColor = phase === 'REPLAY' ? 'white' : 'cyan';
    
    // Player Body (Triangle ship)
    ctx.fillStyle = phase === 'REPLAY' ? 'white' : '#00ffff';
    ctx.beginPath();
    ctx.moveTo(15, 0);
    ctx.lineTo(-10, -10);
    ctx.lineTo(-10, 10);
    ctx.closePath();
    ctx.fill();
    
    ctx.restore();
  };

  // Helper: Circle-Line Intersection
  const circleLineIntersect = (cx: number, cy: number, r: number, x1: number, y1: number, x2: number, y2: number) => {
    const dx = x2 - x1;
    const dy = y2 - y1;
    const lenSq = dx * dx + dy * dy;
    if (lenSq === 0) return false;

    let t = ((cx - x1) * dx + (cy - y1) * dy) / lenSq;
    t = Math.max(0, Math.min(1, t));

    const closestX = x1 + t * dx;
    const closestY = y1 + t * dy;

    const distSq = (cx - closestX) ** 2 + (cy - closestY) ** 2;
    return distSq < r * r;
  };

  return (
    <div className="min-h-screen bg-[#050505] text-white font-sans flex flex-col items-center justify-center p-4 overflow-hidden">
      {/* Header / HUD */}
      <div className="w-full max-w-[800px] flex justify-between items-end mb-4 px-2">
        <div>
          <h1 className="text-4xl font-black tracking-tighter italic text-cyan-400 uppercase leading-none">
            Echo Trace
          </h1>
          <p className="text-[10px] uppercase tracking-widest text-white/40 font-mono mt-1">
            Tactical Replay Protocol v1.0
          </p>
        </div>
        <div className="text-right">
          <div className="text-[10px] uppercase tracking-widest text-white/40 font-mono">Score</div>
          <div className="text-3xl font-mono font-bold leading-none">{score.toString().padStart(6, '0')}</div>
        </div>
      </div>

      {/* Main Game Container */}
      <div className="relative border border-white/10 rounded-2xl overflow-hidden shadow-2xl shadow-cyan-500/10">
        <canvas
          ref={canvasRef}
          width={CANVAS_WIDTH}
          height={CANVAS_HEIGHT}
          className="block bg-black"
        />

        {/* UI Overlays */}
        <AnimatePresence>
          {gameState === 'START' && (
            <motion.div
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
              className="absolute inset-0 bg-black/80 backdrop-blur-sm flex flex-col items-center justify-center text-center p-8"
            >
              <motion.div
                initial={{ scale: 0.9, y: 20 }}
                animate={{ scale: 1, y: 0 }}
                className="max-w-md"
              >
                <div className="w-20 h-20 bg-cyan-500/20 rounded-full flex items-center justify-center mx-auto mb-6 border border-cyan-500/50">
                  <Play className="w-10 h-10 text-cyan-400 fill-cyan-400" />
                </div>
                <h2 className="text-5xl font-black italic uppercase tracking-tighter mb-4">Ready to Trace?</h2>
                <p className="text-white/60 mb-8 text-sm leading-relaxed">
                  Move with <span className="text-cyan-400 font-bold">WASD</span>. Hold <span className="text-cyan-400 font-bold">SPACE</span> to stop time and plan your path. Release to execute and leave a deadly trail.
                </p>
                <button
                  onClick={resetGame}
                  className="px-12 py-4 bg-cyan-500 hover:bg-cyan-400 text-black font-black uppercase tracking-widest rounded-full transition-all hover:scale-105 active:scale-95 shadow-lg shadow-cyan-500/50"
                >
                  Initialize System
                </button>
              </motion.div>
            </motion.div>
          )}

          {gameState === 'GAMEOVER' && (
            <motion.div
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
              className="absolute inset-0 bg-red-950/90 backdrop-blur-md flex flex-col items-center justify-center text-center p-8"
            >
              <motion.div
                initial={{ scale: 0.9, y: 20 }}
                animate={{ scale: 1, y: 0 }}
              >
                <h2 className="text-6xl font-black italic uppercase tracking-tighter text-red-500 mb-2">System Failure</h2>
                <p className="text-white/40 uppercase tracking-[0.3em] text-[10px] mb-8 font-mono">Connection Terminated</p>
                
                <div className="grid grid-cols-2 gap-8 mb-12">
                  <div className="text-left border-l-2 border-white/10 pl-4">
                    <div className="text-[10px] uppercase tracking-widest text-white/40 font-mono">Final Score</div>
                    <div className="text-4xl font-mono font-bold">{score}</div>
                  </div>
                  <div className="text-left border-l-2 border-white/10 pl-4">
                    <div className="text-[10px] uppercase tracking-widest text-white/40 font-mono">High Score</div>
                    <div className="text-4xl font-mono font-bold">{highScore}</div>
                  </div>
                </div>

                <button
                  onClick={resetGame}
                  className="group flex items-center gap-3 px-12 py-4 bg-white text-black font-black uppercase tracking-widest rounded-full transition-all hover:bg-red-500 hover:text-white hover:scale-105 active:scale-95"
                >
                  <RotateCcw className="w-5 h-5 group-hover:rotate-180 transition-transform duration-500" />
                  Reboot System
                </button>
              </motion.div>
            </motion.div>
          )}
        </AnimatePresence>

        {/* Gauge HUD */}
        <div className="absolute bottom-6 left-6 right-6 flex items-center gap-4">
          <div className="flex-1 h-2 bg-white/5 rounded-full overflow-hidden border border-white/10 backdrop-blur-sm">
            <motion.div
              className={`h-full ${phase === 'PLANNING' ? 'bg-cyan-400' : 'bg-cyan-600'}`}
              initial={false}
              animate={{ width: `${timeGauge}%` }}
              transition={{ type: 'spring', bounce: 0, duration: 0.1 }}
            />
          </div>
          <div className="flex items-center gap-2 text-cyan-400">
            <Zap className={`w-4 h-4 ${timeGauge < 20 ? 'animate-pulse text-red-500' : ''}`} />
            <span className={`font-mono text-xs font-bold w-8 ${timeGauge < 20 ? 'text-red-500' : ''}`}>
              {Math.floor(timeGauge)}%
            </span>
          </div>
        </div>

        {/* Phase Indicator */}
        <div className="absolute top-6 left-6">
          <AnimatePresence mode="wait">
            <motion.div
              key={phase}
              initial={{ opacity: 0, x: -10 }}
              animate={{ opacity: 1, x: 0 }}
              exit={{ opacity: 0, x: 10 }}
              className="flex items-center gap-2"
            >
              <div className={`w-2 h-2 rounded-full ${
                phase === 'PLANNING' ? 'bg-cyan-400 animate-pulse' : 
                phase === 'REPLAY' ? 'bg-white animate-ping' : 'bg-cyan-600'
              }`} />
              <span className="text-[10px] uppercase tracking-[0.2em] font-black italic text-white/60">
                {phase} PHASE
              </span>
            </motion.div>
          </AnimatePresence>
        </div>
      </div>

      {/* Footer Info */}
      <div className="mt-6 flex gap-12 text-[10px] uppercase tracking-widest text-white/20 font-mono">
        <div className="flex items-center gap-2">
          <span className="w-2 h-2 bg-cyan-500 rounded-full" />
          <span>Space: Time Stop</span>
        </div>
        <div className="flex items-center gap-2">
          <span className="w-2 h-2 bg-white/40 rounded-full" />
          <span>WASD: Move</span>
        </div>
        <div className="flex items-center gap-2">
          <span className="w-2 h-2 bg-red-500 rounded-full" />
          <span>Avoid: Enemies & Walls</span>
        </div>
      </div>
    </div>
  );
}
