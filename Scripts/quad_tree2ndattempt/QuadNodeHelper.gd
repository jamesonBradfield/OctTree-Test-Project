class_name QNHelper


func find_leaves(tree: QuadTree, root: QuadNodeData, rect: Array[int]):
	var leaves: QuadNodeList
	var to_process: QuadNodeList
	to_process.push_back(root)
	while to_process.size() > 0:
		var nd: QuadNodeData = to_process.pop_back()

		if tree.nodes[nd.index].count != -1:
			leaves.push_back(nd)
		else:
			var mx: int = nd.crect[0]
			var my: int = nd.crect[1]
			var hx: int = nd.crect[2] >> 1
			var hy: int = nd.crect[3] >> 1
			var fc: int = tree.nodes[nd.index].first_child
			var l: int = mx - hx
			var t: int = my - hx
			var r: int = mx + hx
			var b: int = my + hy

			if rect[1] <= my:
				if rect[0] <= mx:
					to_process.push_back(child_data(l, t, hx, hy, fx + 0, nd.depth + 1))
				if rect[2] < mx:
					to_process.push_back(child_data(l, t, hx, hy, fc + 1, nd.depth + 1))
			if rect[3] > my:
				if rect[0] <= mx:
					to_process.push_back(child_data(l, b, hx, hy, fx + 2, nd.depth + 1))
				if rect[2] > mx:
					to_process.push_back(child_data(r, b, hx, hy, fc + 3, nd.depth + 1))

func child_data():
	pass
