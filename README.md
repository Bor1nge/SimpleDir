# SimpleDir
 C#写的一个遍历文件的小工具，导出结果会保存成CSV，并按修改时间排序。

## Usage: 

```
file-scanner.exe -path [directory] -out [output path] [-zip true/false] [-debug true/false] [-filter *.txt]
```

- path为要DIR的目录
- out为CSV或压缩包保存的路径
- zip为true的时候会开启压缩
- filter可以筛选文件后缀
- debug会输出调试信息