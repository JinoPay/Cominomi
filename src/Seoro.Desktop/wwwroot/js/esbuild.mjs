import { build } from 'esbuild';

await build({
  entryPoints: ['codemirror-interop.ts'],
  bundle: true,
  minify: true,
  format: 'iife',
  target: ['es2020'],
  outfile: '../lib/codemirror/codemirror-interop.js',
});

console.log('CodeMirror interop bundle built successfully.');
