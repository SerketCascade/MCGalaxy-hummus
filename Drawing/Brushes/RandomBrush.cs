﻿/*
    Copyright 2015 MCGalaxy
        
    Dual-licensed under the Educational Community License, Version 2.0 and
    the GNU General Public License, Version 3 (the "Licenses"); you may
    not use this file except in compliance with the Licenses. You may
    obtain a copy of the Licenses at
    
    http://www.opensource.org/licenses/ecl2.php
    http://www.gnu.org/licenses/gpl-3.0.html
    
    Unless required by applicable law or agreed to in writing,
    software distributed under the Licenses are distributed on an "AS IS"
    BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
    or implied. See the Licenses for the specific language governing
    permissions and limitations under the Licenses.
 */
using System;
using System.Collections.Generic;
using MCGalaxy.Commands;
using MCGalaxy.Drawing.Ops;

namespace MCGalaxy.Drawing.Brushes {
    
    public sealed class RandomBrush : Brush {
        readonly ExtBlock[] blocks;
        readonly int seed;
        
        public RandomBrush(ExtBlock[] blocks) {
            this.blocks = blocks;
            seed = new Random().Next();
        }
        
        public override string Name { get { return "Random"; } }
        
        public override string[] Help { get { return HelpString; } }
        
        public static string[] HelpString = new [] {
            "%TArguments: [block1/frequency] [block2]..",
            "%HDraws by randomly selecting blocks from the given [blocks].",
            "%Hfrequency is optional (defaults to 1), and specifies the number of times " +
            "the block should appear (as a fraction of the total of all the frequencies).",
        };
        
        public static Brush Process(BrushArgs args) {
            if (args.Message == "")
                return new RandomBrush(new[] { new ExtBlock(args.Type, args.ExtType), 
                                               new ExtBlock(Block.Zero, 0) });
            
            string[] parts = args.Message.Split(' ');
            int[] count = new int[parts.Length];
            ExtBlock[] toAffect = GetBlocks(args.Player, parts.Length, parts, count);
            if (toAffect == null) return null;
            
            ExtBlock[] blocks = Combine(toAffect, count);
            return new RandomBrush(blocks);
        }
        
        static ExtBlock[] GetBlocks(Player p, int max, string[] parts, int[] count) {
            ExtBlock[] blocks = new ExtBlock[max];
            for (int i = 0; i < blocks.Length; i++) {
                blocks[i].Type = Block.Zero;
                count[i] = 1;
            }
            
            for (int i = 0; i < max; i++ ) {
                byte extType = 0;
                int sepIndex = parts[i].IndexOf('/');
                string block = sepIndex >= 0 ? parts[i].Substring(0, sepIndex) : parts[i];
                byte type = DrawCmd.GetBlock(p, block, out extType);
                if (type == Block.Zero) return null;
                
                blocks[i].Type = type; blocks[i].ExtType = extType;
                if (sepIndex < 0) continue;
                int chance;
                if (!int.TryParse(parts[i].Substring(sepIndex + 1), out chance) || chance <= 0 || chance > 10000) {
                    Player.SendMessage(p, "frequency must be an integer between 1 and 10,000."); return null;
                }
                count[i] = chance;
            }
            return blocks;
        }
        
        static ExtBlock[] Combine(ExtBlock[] toAffect, int[] count) {
            int sum = 0;
            for (int i = 0; i < count.Length; i++) sum += count[i];
            if (toAffect.Length == 1) sum += 1;
            
            ExtBlock[] blocks = new ExtBlock[sum];
            for (int i = 0, index = 0; i < toAffect.Length; i++) {
                for (int j = 0; j < count[i]; j++)
                    blocks[index++] = toAffect[i];
            }
            // For one block argument, leave everything else untouched.
            if (toAffect.Length == 1) 
                blocks[blocks.Length - 1] = new ExtBlock(Block.Zero, 0);
            return blocks;
        }
        
        int next;
        const int mask = 0x7fffffff;
        public override byte NextBlock(DrawOp op) {
            // Sourced from http://freespace.virgin.net/hugo.elias/models/m_perlin.htm
            int n = (op.Coords.X + 1217 * op.Coords.Y + 4751 * op.Coords.Z + 673 * seed) & mask;
            n = (n >> 13) ^ n;
            int raw = (n * (n * n * 60493 + 19990303) + 1376312589) & mask;
            next = (int)Math.Floor((raw / (double)mask) * blocks.Length);
            return blocks[next].Type;
        }
        
        public override byte NextExtBlock(DrawOp op) {
            return blocks[next].ExtType;
        }
    }
}
