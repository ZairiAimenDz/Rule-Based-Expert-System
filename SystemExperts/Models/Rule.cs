using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SystemExperts.Models
{
    public class Rule
    {
        public int Number { get; set; }
        public List<string> Inputs { get; set; }
        public List<string> Outputs { get; set; }

        public override string ToString()
        {
            return "R"+Number+" : "+ String.Join(",", Inputs) +"->"+String.Join(",",Outputs);            
        }

        public static Rule StringToRule(string Inputs, string Outputs)
        {
            var rule = new Rule();
            rule.Inputs = Inputs.Trim().Split(',').ToList();
            rule.Outputs = Outputs.Trim().Split(',').ToList();
            return rule;
        }
    }
}
