using Godot;
using System.Collections.Generic;
using System.Linq;
using System;
using MathNet.Numerics.LinearRegression;
using MathNet.Numerics.LinearAlgebra;
namespace Chunk {
  class DualContour {
    List<Vector3> _mids = new();
    List<Vector3> _norms = new();
    Func<Vector3I, Cell> _getCell;
    private Vector3I _dkey;
    public DualContour(Func<Vector3I, Cell> getCell) {
      _getCell = getCell;
    }
    Vector3 QEF(Vector3I cell) {
      var normMat = Matrix<Single>.Build.DenseOfRowArrays(_norms
                  .Select(v => new Single[] { v.X, v.Y, v.Z }));
      return _mids.Aggregate((a, v) => a + v) / _mids.Count;
      if (normMat.ConditionNumber() > 10) {
      }
      var components = Vector<Single>.Build.DenseOfEnumerable(_mids
          .Zip(_norms).Select(pair => pair.First.Dot(pair.Second)));
      Vector<Single> res = MultipleRegression
        .DirectMethod(normMat, components, DirectRegressionMethod.QR);
      return new Vector3(res[0], res[1], res[2]);
    }
    private Single FDelta(SByte dist, SByte pos, SByte neg) {
      var d = (Single)dist / Glob.DistFac;
      var p = (Single)pos / Glob.DistFac;
      var n = (Single)neg / Glob.DistFac;
      Single res = 0;
      if (p < 0 && n != -16) {
        res += p - d;
      }
      if (n < 0 && n != -16) {
        res += d - n;
      }
      if (p < 0 && n < 0) {
        res /= 2;
      }
      return res;
    }
    private Vector3 FNormal(Vector3I cell, Vector3 norm) {
      SByte d = _getCell(cell).Dist;
      if (norm.X == 0) {
        norm.X = FDelta(d,
          _getCell(Vector3I.Right + cell).Dist,
          _getCell(Vector3I.Left + cell).Dist);
      }
      if (norm.Y == 0) {
        norm.Y = FDelta(d,
          _getCell(Vector3I.Up + cell).Dist,
          _getCell(Vector3I.Down + cell).Dist);
      }
      if (norm.Z == 0) {
        norm.Z = FDelta(d,
          _getCell(Vector3I.Back + cell).Dist,
          _getCell(Vector3I.Forward + cell).Dist);
      }
      return norm.Normalized();
    }
    private void AddBias(Vector3I dcell) {
      var massPoint = dcell + Vector3.One * 0.5f;
      var biasStrength = 0.25f;
      _mids.AddRange(Enumerable.Repeat(massPoint, 3));
      _norms.Add(new Vector3(biasStrength, 0, 0));
      _norms.Add(new Vector3(0, biasStrength, 0));
      _norms.Add(new Vector3(0, 0, biasStrength));
    }
    void Boundries(Vector3I dcell) {
      _mids.Clear();
      _norms.Clear();
      // iterate over edges of cube and compare vert distances
      for (int i = 0; i < 3; ++i) {
        for (int j = 0; j < 2; ++j) {
          for (int k = 0; k < 2; ++k) {
            var dcell0 = dcell;
            dcell0[(i + 1) % 3] += j;
            dcell0[(i + 2) % 3] += k;
            var c0 = _getCell(dcell0);
            var dcell1 = dcell0;
            dcell1[i] += 1;
            var c1 = _getCell(dcell1);
            if (c0.Dist == -128 || c1.Dist == -128) {
              continue;
            }
            if (c0.Dist <= 0 && c1.Dist >= 0) {
              var mid = (Vector3)dcell0;
              mid[i] -= (Single)c0.Dist / Glob.DistFac;
              _mids.Add(mid);
              var norm = Vector3.Zero;
              norm[i] = 1;
              norm = FNormal(dcell0, norm);
              _norms.Add(norm);
            } else if (c0.Dist >= 0 && c1.Dist <= 0) {
              var mid = (Vector3)dcell1;
              mid[i] += (Single)c1.Dist / Glob.DistFac;
              _mids.Add(mid);
              var norm = Vector3.Zero;
              norm[i] = -1;
              norm = FNormal(dcell1, norm);
              _norms.Add(norm);
            }
          }
        }
      }
      if (_mids.Count != 0) {
        //var ad = Math.Abs(_getCell(dcell).Dist);
        //if (ad > Glob.DistFac * 4) {
        //for (int i = 0; i < 2; ++i) {
        //for (int j = 0; j < 2; ++j) {
        //for (int k = 0; k < 2; ++k) {
        //var dcell0 = dcell;
        //dcell0 += new Vector3I(i, j, k);
        //GD.PrintErr(dcell, dcell0, _getCell(dcell0).Dist, _getCell(dcell0).GetNormal());
        //}
        //}
        //}
        //}
        //AddBias(dcell);
      }
    }

    public Vector3? ComputeVert(Vector3I dcell, Vector3I dkey) {
      _dkey = dkey;
      Boundries(dcell);
      if (_mids.Count == 0) {
        return null;
      }
      return QEF(dcell);
    }
    public void AddDebug(List<Vector3> vertBuf) {
      for (int i = 0; i < _mids.Count; ++i) {
        vertBuf.Add(_mids[i]);
        vertBuf.Add(_mids[i] + _norms[i]);
      }
    }
  }
}
