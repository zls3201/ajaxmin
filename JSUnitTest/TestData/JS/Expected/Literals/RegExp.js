﻿function test1(text){return/the/.exec(text)}function test2(text){for(var re=/the/g,count=0,match;match=re.exec(text);)++count;return count}